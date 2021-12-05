using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Chain.Events;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;

/// <summary>
/// This class exposes a channel that can be used to publish headers to be validated.
/// This way all peers can download headers and ask for them to be validated.
/// It's not important that all headers published in the channel are consecutive, but it's important
/// that they preserve the chain order of the peer submitting it.
/// E.g. it's fine that multiple peers publish headers at the same time, mixing them, but it's important that
/// these headers are pushed in high order relative to their own branches.
/// Having a simple place where header validation is performed, simplify how concurrency complexity design can be managed.
/// </summary>
public partial class HeaderValidator : IHostedService, IPeriodicWorkExceptionHandler, IHeaderValidator
{
   private readonly Channel<HeadersToValidate> _headersToValidate;
   readonly IPeriodicWork _validationLoop;
   readonly IChainState _chainState;
   readonly IValidationRuleSet<IHeaderValidationRule> _headerValidationRules;
   readonly IHeaderValidationContextFactory _headerValidationContextFactory;
   readonly IEventBus _eventBus;

   readonly UInt256 _genesisHash;

   public HeaderValidator(ILogger<HeaderValidator> logger,
                          IConsensusParameters consensusParameters,
                          IChainState chainState,
                          IValidationRuleSet<IHeaderValidationRule> headerValidationRules,
                          IHeaderValidationContextFactory headerValidationContextFactory,
                          IPeriodicWork validationLoop,
                          IEventBus eventBus)
   {
      _logger = logger;
      _validationLoop = validationLoop;
      _chainState = chainState;
      _headerValidationRules = headerValidationRules;
      _headerValidationContextFactory = headerValidationContextFactory;
      _eventBus = eventBus;

      _headersToValidate = Channel.CreateUnbounded<HeadersToValidate>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
      _genesisHash = consensusParameters.GenesisHeader.Hash!;

      _validationLoop.Configure(false, this);
   }

   public void OnPeriodicWorkException(IPeriodicWork failedWork, Exception ex, ref IPeriodicWorkExceptionHandler.Feedback feedback)
   {
      CriticalPeriodicWorkFailure(failedWork.Label);
      feedback.IsCritical = true;
      feedback.ContinueExecution = false;
      feedback.Message = "Without validation loop, it's impossible to advance in consensus. A node restart is required to fix the problem.";
   }

   public Task StartAsync(CancellationToken cancellationToken)
   {
      _headerValidationRules.SetupRules();

      // starts the consumer loop of header validation
      _ = _validationLoop.StartAsync(
         label: nameof(HeaderValidator),
         work: ValidationWorkAsync,
         interval: TimeSpan.Zero,
         cancellationToken
         );

      return Task.CompletedTask;
   }

   public Task StopAsync(CancellationToken cancellationToken)
   {
      return Task.CompletedTask;
   }

   public async ValueTask RequestValidationAsync(HeadersToValidate header)
   {
      await _headersToValidate.Writer.WriteAsync(header).ConfigureAwait(false);
   }

   /// <summary>
   /// The consumer that perform validation.
   /// </summary>
   /// <param name="cancellation">The cancellation.</param>
   private async Task ValidationWorkAsync(CancellationToken cancellation)
   {
      await foreach (HeadersToValidate request in _headersToValidate.Reader.ReadAllAsync(cancellation))
      {
         if (request.Headers.Count == 0) continue; //if there aren't headers to validate, ignore the request

         DebugValidatingHeaders(request.Headers.Count);

         var newValidatedHeaderNodes = new List<HeaderNode>();

         HeaderNode? lastValidatedHeaderNode = null;
         BlockHeader? lastValidatedBlockHeader = null;
         BlockValidationState? state = null;
         BlockHeader? invalidBlockHeader = null;

         /// If during validation a new header is found, this will be set to true.
         /// Once a new header is found, every other new header is expected to be new too
         /// because we don't store unconnecting headers.
         //bool hasNewHeaders = false;

         int validatedHeaders = 0;

         using (await GlobalLocks.WriteOnMainAsync())
         {
            foreach (BlockHeader header in request.Headers)
            {
               using (_logger.BeginScope("Validating header {ValidationRuleType}", header!.Hash))
               {
                  if (!AcceptBlockHeaderLocked(header, out state, out HeaderNode? validatedHeaderNode, out bool newHeaderFound))
                  {
                     invalidBlockHeader = header;
                     break;
                  }

                  validatedHeaders++;
                  lastValidatedBlockHeader = header;
                  lastValidatedHeaderNode = validatedHeaderNode;
                  if (newHeaderFound)
                  {
                     //hasNewHeaders = true;
                     newValidatedHeaderNodes.Add(validatedHeaderNode);
                  }
               }
            }
         }

         // publish events out of lock
         if (state!.IsInvalid())
         {
            // signal header validation failed
            var eventItem = new BlockHeaderValidationFailed(invalidBlockHeader!, state, request.Peer);
            await _eventBus.PublishAsync(eventItem, cancellation).ConfigureAwait(false);
         }
         else
         {
            // signal header validation succeeded
            var eventItem = new BlockHeaderValidationSucceeded(validatedHeaders,
                                                               lastValidatedBlockHeader!,
                                                               lastValidatedHeaderNode!,
                                                               newValidatedHeaderNodes.Count,
                                                               request.Peer);
            await _eventBus.PublishAsync(eventItem, cancellation).ConfigureAwait(false);
         }
      }
   }

   /// <summary>
   /// Validates the header performing checks for every <see cref="IHeaderValidationRule" /> known rule.
   /// </summary>
   /// <param name="header">The header to be validated.</param>
   /// <param name="validationState">The resulting state of the validation.</param>
   /// <param name="processedHeader">The processed header.</param>
   /// <param name="isNew">if set to <c>true</c> the returned <paramref name="processedHeader"/> is a new header node never seen before.</param>
   /// <returns>
   /// <see langword="true" /> if the validation succeed, <see langword="false" /> otherwise and the reason of the fault
   /// can be found in <paramref name="validationState" />.
   /// </returns>
   private bool AcceptBlockHeaderLocked(BlockHeader header, out BlockValidationState validationState, [MaybeNullWhen(false)] out HeaderNode processedHeader, out bool isNew)
   {
      UInt256 headerHash = header.Hash!;
      validationState = new BlockValidationState();

      //don't validate genesis header
      if (headerHash != _genesisHash)
      {
         IHeaderValidationContext context = _headerValidationContextFactory.Create(header);

         foreach (IHeaderValidationRule rule in _headerValidationRules.Rules)
         {
            if (!rule.Check(context, ref validationState))
            {
               DebugBlockValidationFailed(validationState.ToString());
               isNew = false;
               processedHeader = null;
               return false;
            }

            if (context.IsForcedAsValid)
            {
               isNew = context.KnownHeader == null;

               if (!isNew)
               {
                  processedHeader = context.KnownHeader!;
                  return true;
               }

               break;
            }
         }
      }

      processedHeader = _chainState.AddToBlockIndex(header);
      isNew = true;

      return true;
   }
}


public partial class HeaderValidator
{
   readonly ILogger<HeaderValidator> _logger;

   [LoggerMessage(0, LogLevel.Critical, "An unhandled exception has been raised in the {PeriodicWork} work.")]
   partial void CriticalPeriodicWorkFailure(string periodicWork);

   [LoggerMessage(0, LogLevel.Debug, "Validating {HeadersCount} headers")]
   partial void DebugValidatingHeaders(int headersCount);

   [LoggerMessage(0, LogLevel.Debug, "Header validation failed: {HeaderValidationState}")]
   partial void DebugBlockValidationFailed(string headerValidationState);
}

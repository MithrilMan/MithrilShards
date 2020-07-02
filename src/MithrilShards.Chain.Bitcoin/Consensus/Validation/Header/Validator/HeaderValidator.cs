using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.Events;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   /// <summary>
   /// Experimental class.
   /// This class exposes a channel that can be used to publish headers to be validated.
   /// This way all peers can download headers and ask for them to be validated.
   /// It's not important that all headers published in the channel are consecutive, but it's important
   /// that they preserve the chain order of the peer submitting it.
   /// E.g. it's fine that multiple peers publish headers at the same time, mixing them, but it's important that
   /// these headers are pushed in high order relative to their own branches.
   /// Having a simple place where header validation is performed, simplify how concurrency complexity design can be managed.
   /// </summary>
   public class HeaderValidator : IHostedService, IPeriodicWorkExceptionHandler, IHeaderValidator
   {
      private readonly Channel<HeadersToValidate> headersToValidate;
      private readonly Channel<BlockHeader> headersValidated;
      readonly ILogger<HeaderValidator> logger;
      readonly IPeriodicWork validationLoop;
      readonly IChainState chainState;
      readonly IEnumerable<IHeaderValidationRule> headerValidationRules;
      readonly IHeaderValidationContextFactory headerValidationContextFactory;
      readonly IValidationRulesChecker validationRulesChecker;
      readonly IEventBus eventBus;
      readonly UInt256 genesisHash;

      public HeaderValidator(ILogger<HeaderValidator> logger,
                             IConsensusParameters consensusParameters,
                             IChainState chainState,
                             IEnumerable<IHeaderValidationRule> headerValidationRules,
                             IHeaderValidationContextFactory headerValidationContextFactory,
                             IValidationRulesChecker validationRulesChecker,
                             IPeriodicWork validationLoop,
                             IEventBus eventBus)
      {
         this.logger = logger;
         this.validationLoop = validationLoop;
         this.chainState = chainState;
         this.headerValidationRules = headerValidationRules;
         this.headerValidationContextFactory = headerValidationContextFactory;
         this.validationRulesChecker = validationRulesChecker;
         this.eventBus = eventBus;

         this.headersToValidate = Channel.CreateUnbounded<HeadersToValidate>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
         this.headersValidated = Channel.CreateUnbounded<BlockHeader>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true, });
         this.genesisHash = consensusParameters.GenesisHeader.Hash!;

         this.validationLoop.Configure(false, this);
      }

      public void OnException(IPeriodicWork failedWork, Exception ex, out bool continueExecution)
      {
         this.logger.LogCritical("An unhandled exception has been rised in the header validation loop.");
         continueExecution = false;
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         this.validationRulesChecker.VerifyValidationRules(this.headerValidationRules);

         // starts the consumer loop of header validation
         this.validationLoop.StartAsync(
            label: "HeaderValidator",
            work: ValidationWork,
            interval: TimeSpan.Zero,
            cancellationToken
            );

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }

      public async Task RequestValidationAsync(HeadersToValidate header)
      {
         await this.headersToValidate.Writer.WriteAsync(header).ConfigureAwait(false);
      }

      /// <summary>
      /// The consumer that perform validation.
      /// </summary>
      /// <param name="cancellation">The cancellation.</param>
      private async Task ValidationWork(CancellationToken cancellation)
      {
         await foreach (HeadersToValidate request in headersToValidate.Reader.ReadAllAsync(cancellation))
         {
            if (request.Headers.Count == 0) continue; //if there aren't headers to validate, ignore the request

            this.logger.LogDebug("Validating {HeadersCount} headers", request.Headers.Count);

            HeaderNode? lastValidatedHeaderNode = null;
            BlockHeader? lastValidatedBlockHeader = null;
            BlockValidationState? state = null;
            BlockHeader? invalidBlockHeader = null;

            /// If during validation a new header is found, this will be set to true.
            /// Once a new header is found, every other new header is expected to be new too
            /// because we don't store unconnecting headers.
            bool newHeaderFound = false;
            int validatedHeaders = 0;

            using (var writeLock = GlobalLocks.WriteOnMain())
            {
               foreach (BlockHeader header in request.Headers)
               {
                  if (!this.AcceptBlockHeaderLocked(header, out state, out HeaderNode? validatedHeaderNode, out newHeaderFound))
                  {
                     invalidBlockHeader = header;
                     break;
                  }

                  validatedHeaders++;
                  lastValidatedBlockHeader = header;
                  lastValidatedHeaderNode = validatedHeaderNode;
               }
            }

            // publish events out of lock
            if (state!.IsInvalid())
            {
               // signal header validation failed
               this.eventBus.Publish(new BlockHeaderValidationFailed(invalidBlockHeader!, state, request.Peer));
               //this.MisbehaveDuringHeaderValidation(state, "invalid header received");
               //return false;
            }
            else
            {
               // signal header validation succeeded
               this.eventBus.Publish(new BlockHeaderValidationSucceeded(validatedHeaders,
                                                                        lastValidatedBlockHeader!,
                                                                        lastValidatedHeaderNode!,
                                                                        newHeaderFound,
                                                                        request.Peer));
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
         processedHeader = null!;

         //don't validate genesis header
         if (headerHash != this.genesisHash)
         {
            // check if the tip we want to set is already into our chain
            if (this.chainState.TryGetKnownHeaderNode(headerHash, out HeaderNode? existingHeader))
            {
               if (existingHeader.Validity.HasFlag(HeaderValidityStatuses.FailedMask))
               {
                  validationState.Invalid(BlockValidationFailureContext.BlockCachedInvalid, "duplicate", "block marked as invalid");
                  isNew = false;
                  return false;
               }

               this.logger.LogDebug("The header we want to accept is already in our headers chain.");

               processedHeader = existingHeader;
               isNew = false;
               return true;
            }

            if (header.PreviousBlockHash == null)
            {
               validationState.Invalid(BlockValidationFailureContext.BlockInvalidHeader, "prev-hash-null", "previous hash null, allowed only on genesis block");
               isNew = false;
               return false;
            }

            IHeaderValidationContext context = this.headerValidationContextFactory.Create(header);

            foreach (IHeaderValidationRule rule in this.headerValidationRules)
            {
               if (!rule.Check(context, ref validationState))
               {
                  this.logger.LogDebug("Header validation failed: {HeaderValidationState}", validationState.ToString());
                  isNew = false;
                  return false;
               }
            }
         }

         processedHeader = this.chainState.AddToBlockIndex(header);

         isNew = true;
         return true;
      }
   }
}

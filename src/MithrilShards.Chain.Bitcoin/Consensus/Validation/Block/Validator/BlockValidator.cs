using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Events;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Validator
{
   public class BlockValidator : IHostedService, IPeriodicWorkExceptionHandler, IBlockValidator
   {
      private readonly Channel<BlockToValidate> _blocksToValidate;
      readonly ILogger<BlockValidator> _logger;
      readonly IPeriodicWork _validationLoop;
      readonly IChainState _chainState;
      readonly IValidationRuleSet<IBlockValidationRule> _blockValidationRules;
      readonly IBlockValidationContextFactory _blockValidationContextFactory;
      readonly IEventBus _eventBus;
      readonly UInt256 _genesisHash;

      public BlockValidator(ILogger<BlockValidator> logger,
                             IConsensusParameters consensusParameters,
                             IChainState chainState,
                             IValidationRuleSet<IBlockValidationRule> blockValidationRules,
                             IBlockValidationContextFactory blockValidationContextFactory,
                             IPeriodicWork validationLoop,
                             IEventBus eventBus)
      {
         this._logger = logger;
         this._validationLoop = validationLoop;
         this._chainState = chainState;
         this._blockValidationRules = blockValidationRules;
         this._blockValidationContextFactory = blockValidationContextFactory;
         this._eventBus = eventBus;

         this._blocksToValidate = Channel.CreateUnbounded<BlockToValidate>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
         this._genesisHash = consensusParameters.GenesisHeader.Hash!;

         this._validationLoop.Configure(false, this);
      }

      public void OnPeriodicWorkException(IPeriodicWork failedWork, Exception ex, ref IPeriodicWorkExceptionHandler.Feedback feedback)
      {
         this._logger.LogCritical("An unhandled exception has been raised in the block validation loop.");
         feedback.IsCritical = true;
         feedback.ContinueExecution = false;
         feedback.Message = "Without block validation loop, it's impossible to advance in consensus. A node restart is required to fix the problem.";
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         this._blockValidationRules.SetupRules();

         // starts the consumer loop of header validation
         this._validationLoop.StartAsync(
            label: nameof(BlockValidator),
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

      public async ValueTask RequestValidationAsync(BlockToValidate block)
      {
         await this._blocksToValidate.Writer.WriteAsync(block).ConfigureAwait(false);
      }

      /// <summary>
      /// The consumer that perform validation.
      /// </summary>
      /// <param name="cancellation">The cancellation.</param>
      private async Task ValidationWorkAsync(CancellationToken cancellation)
      {
         await foreach (BlockToValidate request in _blocksToValidate.Reader.ReadAllAsync(cancellation))
         {
            using (IDisposable logScope = _logger.BeginScope("Validating block {BlockHash}", request.Block.Header!.Hash))
            {

               BlockValidationState? state = null;

               bool isNew = false;
               using (await GlobalLocks.WriteOnMainAsync())
               {
                  this.AcceptBlockLocked(request.Block, out state, out isNew);
               }

               // publish events out of lock
               if (state!.IsInvalid())
               {
                  // signal header validation failed
                  this._eventBus.Publish(new BlockValidationFailed(request.Block, state, request.Peer));
               }
               else
               {
                  // signal header validation succeeded
                  this._eventBus.Publish(new BlockValidationSucceeded(request.Block, isNew, request.Peer));
               }
            }
         }
      }


      private bool AcceptBlockLocked(Protocol.Types.Block block, out BlockValidationState validationState, out bool isNew)
      {
         validationState = new BlockValidationState();

         IBlockValidationContext context = this._blockValidationContextFactory.Create(block);

         foreach (IBlockValidationRule rule in this._blockValidationRules.Rules)
         {
            if (!rule.Check(context, ref validationState))
            {
               this._logger.LogDebug("Block validation failed: {BlockValidationState}", validationState.ToString());
               isNew = false;
               return false;
            }

            if (context.IsForcedAsValid)
            {
               isNew = context.KnownBlock == null;

               break;
            }
         }
         isNew = true;

         return true;
      }
   }
}

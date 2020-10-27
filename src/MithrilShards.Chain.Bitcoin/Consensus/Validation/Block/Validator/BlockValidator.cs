using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
      private readonly Channel<BlockToValidate> blocksToValidate;
      readonly ILogger<BlockValidator> logger;
      readonly IPeriodicWork validationLoop;
      readonly IChainState chainState;
      readonly IValidationRuleSet<IBlockValidationRule> blockValidationRules;
      readonly IBlockValidationContextFactory blockValidationContextFactory;
      readonly IEventBus eventBus;
      readonly UInt256 genesisHash;

      public BlockValidator(ILogger<BlockValidator> logger,
                             IConsensusParameters consensusParameters,
                             IChainState chainState,
                             IValidationRuleSet<IBlockValidationRule> blockValidationRules,
                             IBlockValidationContextFactory blockValidationContextFactory,
                             IPeriodicWork validationLoop,
                             IEventBus eventBus)
      {
         this.logger = logger;
         this.validationLoop = validationLoop;
         this.chainState = chainState;
         this.blockValidationRules = blockValidationRules;
         this.blockValidationContextFactory = blockValidationContextFactory;
         this.eventBus = eventBus;

         this.blocksToValidate = Channel.CreateUnbounded<BlockToValidate>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
         this.genesisHash = consensusParameters.GenesisHeader.Hash!;

         this.validationLoop.Configure(false, this);
      }

      public void OnException(IPeriodicWork failedWork, Exception ex, ref IPeriodicWorkExceptionHandler.Feedback feedback)
      {
         this.logger.LogCritical("An unhandled exception has been raised in the block validation loop.");
         feedback.IsCritical = true;
         feedback.ContinueExecution = false;
         feedback.Message = "Without block validation loop, it's impossible to advance in consensus. A node restart is required to fix the problem.";
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         this.blockValidationRules.SetupRules();

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

      public async ValueTask RequestValidationAsync(BlockToValidate block)
      {
         await this.blocksToValidate.Writer.WriteAsync(block).ConfigureAwait(false);
      }

      /// <summary>
      /// The consumer that perform validation.
      /// </summary>
      /// <param name="cancellation">The cancellation.</param>
      private async Task ValidationWork(CancellationToken cancellation)
      {
         await foreach (BlockToValidate request in blocksToValidate.Reader.ReadAllAsync(cancellation))
         {
            using (IDisposable logScope = logger.BeginScope("Validating block {BlockHash}", request.Block.Header!.Hash))
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
                  this.eventBus.Publish(new BlockValidationFailed(request.Block, state, request.Peer));
               }
               else
               {
                  // signal header validation succeeded
                  this.eventBus.Publish(new BlockValidationSucceeded(request.Block, isNew, request.Peer));
               }
            }
         }
      }


      private bool AcceptBlockLocked(Protocol.Types.Block block, out BlockValidationState validationState, out bool isNew)
      {
         validationState = new BlockValidationState();

         IBlockValidationContext context = this.blockValidationContextFactory.Create(block);

         foreach (IBlockValidationRule rule in this.blockValidationRules.Rules)
         {
            if (!rule.Check(context, ref validationState))
            {
               this.logger.LogDebug("Block validation failed: {BlockValidationState}", validationState.ToString());
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

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

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Validator;

public class BlockValidator : IHostedService, IPeriodicWorkExceptionHandler, IBlockValidator
{
   private readonly Channel<BlockToValidate> _blocksToValidate;
   readonly ILogger<BlockValidator> _logger;
   readonly IPeriodicWork _validationLoop;
   readonly IChainState _chainState;
   readonly IValidationRuleSet<IBlockValidationRule> _blockValidationRules;
   readonly IBlockValidationContextFactory _blockValidationContextFactory;
   readonly IEventBus _eventBus;
   readonly ICoinsView _coinsView; // Main UTXO set view
   readonly UInt256 _genesisHash;

   public BlockValidator(ILogger<BlockValidator> logger,
                          IConsensusParameters consensusParameters,
                          IChainState chainState,
                          IValidationRuleSet<IBlockValidationRule> blockValidationRules,
                          IBlockValidationContextFactory blockValidationContextFactory,
                          IPeriodicWork validationLoop,
                          IEventBus eventBus,
                          ICoinsView coinsView) // Inject ICoinsView
   {
      _logger = logger;
      _validationLoop = validationLoop;
      _chainState = chainState;
      _blockValidationRules = blockValidationRules;
      _blockValidationContextFactory = blockValidationContextFactory;
      _eventBus = eventBus;
      _coinsView = coinsView; // Store injected ICoinsView
      _blocksToValidate = Channel.CreateUnbounded<BlockToValidate>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
      _genesisHash = consensusParameters.GenesisHeader.Hash!;

      _validationLoop.Configure(false, this);
   }

   public void OnPeriodicWorkException(IPeriodicWork failedWork, Exception ex, ref IPeriodicWorkExceptionHandler.Feedback feedback)
   {
      _logger.LogCritical("An unhandled exception has been raised in the block validation loop.");
      feedback.IsCritical = true;
      feedback.ContinueExecution = false;
      feedback.Message = "Without block validation loop, it's impossible to advance in consensus. A node restart is required to fix the problem.";
   }

   public Task StartAsync(CancellationToken cancellationToken)
   {
      _blockValidationRules.SetupRules();

      // starts the consumer loop of header validation
      _ = _validationLoop.StartAsync(
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
      await _blocksToValidate.Writer.WriteAsync(block).ConfigureAwait(false);
   }

   /// <summary>
   /// The consumer that perform validation.
   /// </summary>
   /// <param name="cancellation">The cancellation.</param>
   private async Task ValidationWorkAsync(CancellationToken cancellation)
   {
      await foreach (BlockToValidate request in _blocksToValidate.Reader.ReadAllAsync(cancellation))
      {
         using var _ = _logger.BeginScope("Validating block {BlockHash}", request.Block.Header!.Hash);

         BlockValidationState? state = null;

         bool isNew = false;
         using (await GlobalLocks.WriteOnMainAsync())
         {
            AcceptBlockLocked(request.Block, out state, out isNew);
         }

         // publish events out of lock
         if (state!.IsInvalid())
         {
            // signal header validation failed
            await _eventBus.PublishAsync(new BlockValidationFailed(request.Block, state, request.Peer), cancellation).ConfigureAwait(false);
         }
         else
         {
            // signal header validation succeeded
            await _eventBus.PublishAsync(new BlockValidationSucceeded(request.Block, isNew, request.Peer), cancellation).ConfigureAwait(false);
         }
      }
   }


   private bool AcceptBlockLocked(Protocol.Types.Block block, out BlockValidationState validationState, out bool isNew)
   {
      validationState = new BlockValidationState();

      // TODO: Here, we should ideally create a CoinsViewCache based on the current state (e.g., _chainState.Tip)
      // and the block being validated. For this task, we'll pass the main _coinsView directly.
      // In a real system, this would be something like:
      // var parentBlockIndex = _chainState.GetHeaderNode(block.Header.PreviousBlockHash);
      // ICoinsView blockSpecificView = new CoinsViewCache(_coinsView, parentBlockIndex);
      // For now, using the injected _coinsView. This will be an issue for blocks that spend outputs from the same block.
      // This simplification means intra-block spends won't be visible to rules if _coinsView is a direct DB view.
      // A proper CoinsViewCache that can be updated by rules or by a pre-pass is needed for full validation.

      // Assuming CoinsViewCache can be instantiated with a base ICoinsView.
      // The lifetime of this cache would be per-block validation.
      // If CoinsViewCache does not exist, its creation would be a prerequisite.
      // For this step, we demonstrate its intended use.
      CoinsViewCache viewForContext = new CoinsViewCache(_coinsView);

      IBlockValidationContext context = _blockValidationContextFactory.Create(block, viewForContext);

      foreach (IBlockValidationRule rule in _blockValidationRules.Rules)
      {
         if (!rule.Check(context, ref validationState))
         {
            _logger.LogDebug("Block validation failed: {BlockValidationState}", validationState.ToString());
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

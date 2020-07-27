using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block
{
   /// <summary>
   /// Allows to create instances of <see cref="IBlockValidationContext"/> needed during consensus validation.
   /// </summary>
   public class BlockValidationContextFactory : IBlockValidationContextFactory
   {
      readonly ILogger<BlockValidationContextFactory> logger;
      readonly IInitialBlockDownloadTracker initialBlockDownloadState;
      readonly IChainState chainState;
      readonly IConsensusParameters consensusParameters;

      public BlockValidationContextFactory(ILogger<BlockValidationContextFactory> logger,
                                            IInitialBlockDownloadTracker initialBlockDownloadState,
                                            IChainState chainState,
                                            IConsensusParameters consensusParameters)
      {
         this.logger = logger;
         this.initialBlockDownloadState = initialBlockDownloadState;
         this.chainState = chainState;
         this.consensusParameters = consensusParameters;
      }

      public IBlockValidationContext Create(Protocol.Types.Block block)
      {
         return new BlockValidationContext(logger, block, this.initialBlockDownloadState.IsDownloadingBlocks(), this.chainState, this.consensusParameters);
      }
   }
}
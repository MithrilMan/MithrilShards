using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block
{
   /// <summary>
   /// Allows to create instances of <see cref="IBlockValidationContext"/> needed during consensus validation.
   /// </summary>
   public class BlockValidationContextFactory : IBlockValidationContextFactory
   {
      readonly ILogger<BlockValidationContextFactory> _logger;
      readonly IInitialBlockDownloadTracker _initialBlockDownloadState;
      readonly IChainState _chainState;

      public BlockValidationContextFactory(ILogger<BlockValidationContextFactory> logger,
                                            IInitialBlockDownloadTracker initialBlockDownloadState,
                                            IChainState chainState)
      {
         this._logger = logger;
         this._initialBlockDownloadState = initialBlockDownloadState;
         this._chainState = chainState;
      }

      public IBlockValidationContext Create(Protocol.Types.Block block)
      {
         return new BlockValidationContext(_logger, block, this._initialBlockDownloadState.IsDownloadingBlocks(), this._chainState);
      }
   }
}
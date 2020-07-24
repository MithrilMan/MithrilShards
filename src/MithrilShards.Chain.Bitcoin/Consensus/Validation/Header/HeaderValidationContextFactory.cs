using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   /// <summary>
   /// Allows to create instances of <see cref="IHeaderValidationContext"/> needed during consensus validation.
   /// </summary>
   public class HeaderValidationContextFactory : IHeaderValidationContextFactory
   {
      readonly ILogger<HeaderValidationContextFactory> logger;
      readonly IInitialBlockDownloadTracker initialBlockDownloadState;
      readonly IConsensusParameters consensusParameters;
      readonly IChainState chainState;

      public HeaderValidationContextFactory(ILogger<HeaderValidationContextFactory> logger,
                                            IInitialBlockDownloadTracker initialBlockDownloadState,
                                            IConsensusParameters consensusParameters,
                                            IChainState chainState)
      {
         this.logger = logger;
         this.initialBlockDownloadState = initialBlockDownloadState;
         this.consensusParameters = consensusParameters;
         this.chainState = chainState;
      }

      public IHeaderValidationContext Create(BlockHeader header)
      {
         return new HeaderValidationContext(logger, header, this.initialBlockDownloadState.IsDownloadingBlocks(), this.chainState, this.consensusParameters);
      }
   }
}
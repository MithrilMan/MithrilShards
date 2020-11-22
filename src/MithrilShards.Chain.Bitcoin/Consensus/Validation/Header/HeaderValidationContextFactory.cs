using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   /// <summary>
   /// Allows to create instances of <see cref="IHeaderValidationContext"/> needed during consensus validation.
   /// </summary>
   public class HeaderValidationContextFactory : IHeaderValidationContextFactory
   {
      readonly ILogger<HeaderValidationContextFactory> _logger;
      readonly IInitialBlockDownloadTracker _initialBlockDownloadState;
      readonly IChainState _chainState;

      public HeaderValidationContextFactory(ILogger<HeaderValidationContextFactory> logger,
                                            IInitialBlockDownloadTracker initialBlockDownloadState,
                                            IChainState chainState)
      {
         this._logger = logger;
         this._initialBlockDownloadState = initialBlockDownloadState;
         this._chainState = chainState;
      }

      public IHeaderValidationContext Create(BlockHeader header)
      {
         return new HeaderValidationContext(_logger, header, this._initialBlockDownloadState.IsDownloadingBlocks(), this._chainState);
      }
   }
}
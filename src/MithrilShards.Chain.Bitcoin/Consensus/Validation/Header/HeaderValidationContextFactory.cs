using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   /// <summary>
   /// Allows to create instances of <see cref="IHeaderValidationContext"/> needed during consensus validation.
   /// </summary>
   public class HeaderValidationContextFactory : IHeaderValidationContextFactory
   {
      readonly ILogger<HeaderValidationContextFactory> logger;
      readonly IInitialBlockDownloadState initialBlockDownloadState;

      public HeaderValidationContextFactory(ILogger<HeaderValidationContextFactory> logger, IInitialBlockDownloadState initialBlockDownloadState)
      {
         this.logger = logger;
         this.initialBlockDownloadState = initialBlockDownloadState;
      }

      public IHeaderValidationContext Create(BlockHeader header)
      {
         return new HeaderValidationContext(header, this.initialBlockDownloadState.isInIBD);
      }
   }
}
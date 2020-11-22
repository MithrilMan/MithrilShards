using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public class HeaderValidationContext : ValidationContext, IHeaderValidationContext
   {

      public BlockHeader Header { get; }

      private readonly HeaderNode? _knownHeader = null;

      public HeaderNode? KnownHeader => _knownHeader;

      /// <summary>
      /// Initializes a new instance of the <see cref="HeaderValidationContext"/> class.
      /// </summary>
      /// <param name="header">The header to be validated.</param>
      /// <param name="isInInitialBlockDownloadState">if set to <c>true</c> node is currently in InitialBlockDownload state.</param>
      public HeaderValidationContext(ILogger logger,
                                     BlockHeader header,
                                     bool isInInitialBlockDownloadState,
                                     IChainState chainState) : base(logger, isInInitialBlockDownloadState, chainState)
      {
         this.Header = header;
         this.ChainState.TryGetKnownHeaderNode(header.Hash, out _knownHeader);
      }
   }
}
using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public class HeaderValidationContext : IHeaderValidationContext
   {
      public BlockHeader Header { get; }

      public bool IsInInitialBlockDownloadState { get; }

      public IChainState ChainState { get; }

      public Dictionary<object, object> Items => new Dictionary<object, object>();

      /// <summary>
      /// Initializes a new instance of the <see cref="HeaderValidationContext"/> class.
      /// </summary>
      /// <param name="header">The header.</param>
      /// <param name="isInInitialBlockDownloadState">if set to <c>true</c> node is currently in InitialBlockDownload state.</param>
      public HeaderValidationContext(BlockHeader header, bool isInInitialBlockDownloadState, IChainState chainState)
      {
         this.Header = header;
         this.IsInInitialBlockDownloadState = isInInitialBlockDownloadState;
         this.ChainState = chainState;
      }
   }
}
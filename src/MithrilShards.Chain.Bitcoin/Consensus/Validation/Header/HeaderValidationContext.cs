using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public class HeaderValidationContext : IHeaderValidationContext
   {
      public BlockHeader Header { get; }

      /// <summary>
      /// Gets a value indicating whether this instance is in initial block download state.
      /// </summary>
      /// <value>
      ///   <c>true</c> if this instance is in initial block download state; otherwise, <c>false</c>.
      /// </value>
      public bool IsInInitialBlockDownloadState { get; }

      /// <summary>
      /// Gets a value indicating whether the validating header is instance is known header.
      /// </summary>
      /// <value>
      ///   <c>true</c> if this instance is known header; otherwise, <c>false</c>.
      /// </value>
      public bool IsKnownHeader { get; }

      /// <summary>
      /// Gets the headers tree.
      /// </summary>
      public HeadersTree HeadersTree { get; }

      /// <summary>
      /// Gets the items collection.
      /// Items collection is used to pass data among different rules.
      /// </summary>
      public Dictionary<object, object> Items => new Dictionary<object, object>();


      /// <summary>
      /// Initializes a new instance of the <see cref="HeaderValidationContext"/> class.
      /// </summary>
      /// <param name="header">The header.</param>
      /// <param name="isInInitialBlockDownloadState">if set to <c>true</c> node is currently in InitialBlockDownload state.</param>
      public HeaderValidationContext(BlockHeader header, bool isInInitialBlockDownloadState, HeadersTree headersTree)
      {
         this.Header = header;
         this.IsInInitialBlockDownloadState = isInInitialBlockDownloadState;
         this.HeadersTree = headersTree;
      }
   }
}

using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public interface IHeaderValidationContext
   {
      BlockHeader Header { get; }

      /// <summary>
      /// Gets the items collection used to exchange data/mid-states between different rules.
      /// </summary>
      /// <value>
      /// The collection of items stored in the validation context.
      /// </value>
      Dictionary<object, object> Items { get; }

      /// <summary>
      /// Gets the headers tree that represents currently known headers.
      /// </summary>
      /// <value>
      /// The headers tree.
      /// </value>
      HeadersTree HeadersTree { get; }

      /// <summary>
      /// Gets a value indicating whether this instance is in initial block download state.
      /// </summary>
      /// <value>
      ///   <c>true</c> if this instance is in initial block download state; otherwise, <c>false</c>.
      /// </value>
      public bool IsInInitialBlockDownloadState { get; }
   }
}
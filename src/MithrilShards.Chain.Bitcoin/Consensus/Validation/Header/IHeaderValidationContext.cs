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
      /// Gives access to current block chain state and known headers
      /// (TODO: abstract with an interface that acts as a subset of what ChainState can do)
      /// </summary>
      /// <value>
      /// The headers tree.
      /// </value>
      IChainState ChainState { get; }

      /// <summary>
      /// Gets a value indicating whether this instance is in initial block download state.
      /// </summary>
      /// <value>
      ///   <c>true</c> if this instance is in initial block download state; otherwise, <c>false</c>.
      /// </value>
      public bool IsInInitialBlockDownloadState { get; }
   }
}
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public interface IHeaderValidationContext
   {
      /// <summary>
      /// Gets the header to be validated.
      /// </summary>
      /// <value>
      /// The header.
      /// </value>
      BlockHeader Header { get; }

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
      bool IsInInitialBlockDownloadState { get; }

      /// <summary>
      /// Set data into the context data set.
      /// Used as a mechanism to exchange data/mid-states between different rules.
      /// </summary>
      /// <typeparam name="T">The type of the object stored in context data.</typeparam>
      /// <param name="key">The key.</param>
      /// <param name="data">The data.</param>
      /// <seealso cref=""/>
      void SetData<T>(string key, T data) where T : notnull;

      /// <summary>
      /// Tries to get data typed data from the context data set.
      /// Used as a mechanism to exchange data/mid-states between different rules.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="key">The key.</param>
      /// <param name="data">The data.</param>
      /// <returns></returns>
      bool TryGetData<T>(string key, [MaybeNullWhen(false)] out T data);
   }
}
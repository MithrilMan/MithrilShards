using System.Diagnostics.CodeAnalysis;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   /// <summary>
   /// Validation Context interface that defines common interface to be extended by more specific validation context like header and block validation contexts.
   /// </summary>
   public interface IValidationContext
   {
      /// <summary>
      /// Gets the chain consensus parameters.
      /// </summary>
      IConsensusParameters ConsensusParameters { get; }

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

      /// <summary>
      /// Sets the contextual validating item as valid and stops the validation process.
      /// </summary>
      /// <remarks>Use it wisely because this would prevent other rules to be checked.</remarks>
      void ForceAsValid(string reason);

      /// <summary>
      /// Gets a value indicating whether this validating item has been forced as valid by <see cref="ForceAsValid"/>.
      /// </summary>
      /// <value>
      ///   <c>true</c> if the contextual validating item is forced as valid; otherwise, <c>false</c>.
      /// </value>
      bool IsForcedAsValid { get; }
   }
}

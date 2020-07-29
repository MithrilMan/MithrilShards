using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   public abstract class ValidationContext : IValidationContext
   {
      protected readonly ILogger logger;

      public bool IsInInitialBlockDownloadState { get; }

      public IChainState ChainState { get; }

      public bool IsForcedAsValid { get; private set; } = false;

      protected Dictionary<string, object> items = new Dictionary<string, object>();

      /// <summary>
      /// Initializes a new instance of the <see cref="ValidationContext" /> implementation.
      /// </summary>
      /// <param name="logger">The logger.</param>
      /// <param name="isInInitialBlockDownloadState">if set to <c>true</c> node is currently in InitialBlockDownload state.</param>
      /// <param name="chainState">State of the chain.</param>
      public ValidationContext(ILogger logger, bool isInInitialBlockDownloadState, IChainState chainState)
      {
         this.logger = logger;
         this.IsInInitialBlockDownloadState = isInInitialBlockDownloadState;
         this.ChainState = chainState;
      }

      public void SetData<T>(string key, T data) where T : notnull
      {
         if (this.items.ContainsKey(key))
         {
            this.logger.LogDebug("Overwriting context data {DataKey}", key);
         }
         this.items[key] = data;
      }

      public bool TryGetData<T>(string key, [MaybeNullWhen(false)] out T data)
      {
         if (!this.items.ContainsKey(key))
         {
            this.logger.LogDebug("context data not found: {DataKey}", key);
            data = default(T);
            return false;
         }

         //here may throw if the stored type is not the same as the passed T
         data = (T)this.items[key];

         return true;
      }

      public void ForceAsValid(string reason)
      {
         this.logger.LogDebug("Forced as valid because {ForceAsValidReason}", reason);
         this.IsForcedAsValid = true;
      }
   }
}

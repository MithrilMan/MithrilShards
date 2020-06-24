using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public class HeaderValidationContext : IHeaderValidationContext
   {
      readonly ILogger logger;

      public BlockHeader Header { get; }

      public bool IsInInitialBlockDownloadState { get; }

      public IChainState ChainState { get; }

      protected Dictionary<string, object> items = new Dictionary<string, object>();

      /// <summary>
      /// Initializes a new instance of the <see cref="HeaderValidationContext"/> class.
      /// </summary>
      /// <param name="header">The header to be validated.</param>
      /// <param name="isInInitialBlockDownloadState">if set to <c>true</c> node is currently in InitialBlockDownload state.</param>
      public HeaderValidationContext(ILogger logger, BlockHeader header, bool isInInitialBlockDownloadState, IChainState chainState)
      {
         this.logger = logger;
         this.Header = header;
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
   }
}
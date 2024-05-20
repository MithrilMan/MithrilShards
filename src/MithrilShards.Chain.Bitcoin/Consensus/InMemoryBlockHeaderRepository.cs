using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Chain.Events;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Consensus;

/// <summary>
/// In-Memory implementation of <see cref="IBlockHeaderRepository" />, not suitable for production.
/// </summary>
/// <seealso cref="IBlockHeaderRepository" />
public class InMemoryBlockHeaderRepository(
   ILogger<InMemoryBlockHeaderRepository> logger,
   IEventBus eventBus
   ) : IBlockHeaderRepository, IDisposable
{
   private readonly Dictionary<UInt256, BlockHeader> _headers = [];
   private readonly ReaderWriterLockSlim _theLock = new();

   /// <summary>
   /// Tries to add a header to the repository.
   /// </summary>
   /// <param name="header">The header to add.</param>
   /// <returns>
   ///   <see langword="true" /> if the header has been added, <see langword="false" /> if it was already in the repository.
   /// </returns>
   /// <exception cref="ArgumentNullException">header</exception>
   /// <exception cref="NullReferenceException">Block Header hash cannot be null</exception>
   public async ValueTask<bool> TryAddAsync(BlockHeader header)
   {
      ArgumentNullException.ThrowIfNull(header);

      logger.LogDebug("Adding block header {BlockHash} to repository", header.Hash);

      UInt256? hash = header.Hash ?? throw new NullReferenceException("Block Header hash cannot be null");
      bool success;
      using (new WriteLock(_theLock))
      {
         if (_headers.ContainsKey(hash))
         {
            success = false;
         }
         else
         {
            _headers[hash] = header;
            success = true;
         }
      }

      if (success)
      {
         await eventBus.PublishAsync(new BlockHeaderAddedToRepository(header)).ConfigureAwait(false);
      }

      return success;
   }

   /// <summary>
   /// Gets the <see cref="BlockHeader" /> with specified hash.
   /// </summary>
   /// <param name="hash">The hash.</param>
   /// <param name="header">The header.</param>
   /// <returns>
   ///   <see langword="true" /> if the header has been found, <see langword="false" /> otherwise
   /// </returns>
   public bool TryGet(UInt256 hash, [MaybeNullWhen(false)] out BlockHeader header)
   {
      ArgumentNullException.ThrowIfNull(hash);

      using (new ReadLock(_theLock))
      {
         return _headers.TryGetValue(hash, out header!);
      }
   }

   public void Dispose() {
      _theLock.Dispose();
   }
}

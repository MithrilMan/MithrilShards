using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// A thread safe headers tree that tracks current best chain and known headers on forks.
   /// A List holds the best chain headers sorted by height starting from genesis and a dictionary holds relationships between known block
   /// hashes and their previous hash and the height where that hash was/is on the best chain.
   /// </summary>
   /// <remarks>
   /// Internally it uses <see cref="ReaderWriterLockSlim"/> to ensure thread safety on every get and set method.
   /// </remarks>
   public class HeadersTree : IHeadersTree
   {
      private const int INITIAL_ITEMS_ALLOCATED = 16 ^ 2; //this parameter may go into settings, better to be multiple of 2

      private readonly ReaderWriterLockSlim _theLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

      private readonly ILogger<HeadersTree> _logger;
      private readonly IConsensusParameters _consensusParameters;

      /// <summary>
      /// Known set of hashes, both on forks and on best chains.
      /// Those who are on the best chain, can be found in the <see cref="_bestChain"/> list.
      /// </summary>
      private readonly Dictionary<UInt256, HeaderNode> _knownHeaders = new Dictionary<UInt256, HeaderNode>(INITIAL_ITEMS_ALLOCATED);

      /// <summary>
      /// The best chain of hashes sorted by height.
      /// If a block hash is in this list, it means it's in the main chain and can be sent to other peers.
      /// </summary>
      private readonly List<UInt256> _bestChain = new List<UInt256>(INITIAL_ITEMS_ALLOCATED);

      /// <summary>
      /// The genesis node.
      /// </summary>
      public HeaderNode Genesis { get; }

      private int _height;

      public HeadersTree(ILogger<HeadersTree> logger, IConsensusParameters consensusParameters)
      {
         this._logger = logger;
         this._consensusParameters = consensusParameters ?? throw new ArgumentNullException(nameof(consensusParameters));

         this.Genesis = HeaderNode.GenerateGenesis(this._consensusParameters.GenesisHeader);

         this.ResetToGenesis();
      }

      private void ResetToGenesis()
      {
         using (new WriteLock(this._theLock))
         {
            this._bestChain.Clear();
            this._knownHeaders.Clear();

            this._height = 0;
            this._bestChain.Add(this.Genesis.Hash);
            this._knownHeaders.Add(this.Genesis.Hash, this.Genesis);
         }
      }

      /// <summary>
      /// Tries to get the <see cref="HeaderNode" /> giving its hash.
      /// </summary>
      /// <param name="blockHash">The block hash.</param>
      /// <param name="onlyBestChain">if set to <c>true</c> check only headers that belong to the best chain.</param>
      /// <param name="node">The header node having the passed <paramref name="blockHash"/>.</param>
      /// <returns>
      ///   <c>true</c> if the result has been found, <see langword="false" /> otherwise.
      /// </returns>
      public bool TryGetNode(UInt256? blockHash, bool onlyBestChain, [MaybeNullWhen(false)] out HeaderNode node)
      {
         if (blockHash == null)
         {
            node = null;
            return false;
         }

         using (new ReadLock(this._theLock))
         {
            return onlyBestChain ? this.TryGetNodeOnBestChainNoLock(blockHash, out node!) : this._knownHeaders.TryGetValue(blockHash, out node!);
         }
      }

      /// <summary>
      /// Tries to get the <see cref="HeaderNode" /> on best chain at a specified height.
      /// </summary>
      /// <param name="height">The height.</param>
      /// <param name="node">The header node having the passed <paramref name="blockHash"/>.</param>
      /// <returns>
      ///   <c>true</c> if the result has been found, <see langword="false" /> otherwise.
      /// </returns>
      public bool TryGetNodeOnBestChain(int height, [MaybeNullWhen(false)] out HeaderNode node)
      {
         using (new ReadLock(this._theLock))
         {
            if (height > this._height)
            {
               node = null!;
               return false;
            }

            node = this.GetHeaderNodeNoLock(height);
            return true;
         }
      }

      public void Add(in HeaderNode newHeader)
      {
         using (new WriteLock(this._theLock))
         {
            this._knownHeaders.Add(newHeader.Hash, newHeader);
         }
      }
      public void SetTip(HeaderNode newTip)
      {
         //TODO
         /// only this method have to alter bestChain, all other place should be removed (except if we implement a RewindTip but even in
         /// that case it should probably call internally this method
      }



      ///// <summary>
      ///// Set a new tip in the chain
      ///// </summary>
      ///// <param name="newTip">The new tip</param>
      ///// <param name="newTipPreviousHash">The block hash before the new tip</param>
      //public ConnectHeaderResult TrySetTip(in BlockHeader newTip, ref BlockValidationState validationState)
      //{
      //   UInt256 newTipHash = newTip.Hash!;
      //   UInt256? newTipPreviousHash = newTip.PreviousBlockHash;

      //   using (new WriteLock(this.theLock))
      //   {
      //      // check if the tip we want to set is already into our chain
      //      if (this.knownHeaders.TryGetValue(newTipHash, out HeaderNode? tipNode))
      //      {
      //         if (tipNode.Validity.HasFlag(HeaderValidityStatuses.FailedMask))
      //         {
      //            validationState.Invalid(BlockValidationFailureContext.BlockCachedInvalid, "duplicate", "block marked as invalid");
      //            return this.ValidationFailure(validationState);
      //         }
      //      }


      //      continue L3612 validation.cpp

      //      // if newTipPreviousHash isn't current tip, means we need to rollback
      //      bool needRewind = this.height != newTipPreviousHeader.Height;
      //      if (needRewind)
      //      {
      //         int rollingBackHeight = this.height;
      //         while (rollingBackHeight > newTipPreviousHeader.Height)
      //         {
      //            this.knownHeaders.Remove(this.bestChain[rollingBackHeight]);
      //            this.bestChain.RemoveAt(rollingBackHeight);
      //            rollingBackHeight--;
      //         }
      //         this.height = rollingBackHeight;
      //      }

      //      // now we can put the tip on top of our chain.
      //      this.height++;
      //      this.bestChain.Add(newTipHash);
      //      this.knownHeaders.Add(newTipHash, new HeaderNode(this.height, newTipHash, newTipPreviousHash));
      //      this.blockHeaderRepository.TryAdd(newTip);

      //      return needRewind ? ConnectHeaderResult.Rewinded : ConnectHeaderResult.Connected;
      //   }
      //}

      public BlockLocator? GetLocator(HeaderNode? headerNode)
      {
         using var readLock = new ReadLock(this._theLock);

         if (headerNode == null)
         {
            headerNode = this.GetTip();
         }

         var hashes = new List<UInt256>(32); //sets initial capacity to a number that can fit usual case
         int step = 1;

         while (headerNode != null)
         {
            hashes.Add(headerNode.Hash);

            // Stop when we have added the genesis block.
            if (headerNode.Height == 0) break;

            // Exponentially larger steps back, plus the genesis block.
            int height = Math.Max(headerNode.Height - step, 0);
            if (this.IsInBestChain(headerNode))
            {
               // Use O(1) CChain index if possible.
               this.TryGetNodeOnBestChain(height, out headerNode);
            }
            else
            {
               // Otherwise, use O(log n) skiplist.
               headerNode = headerNode.GetAncestor(height);
            }

            if (hashes.Count > 10) step *= 2;
         }

         return new BlockLocator { BlockLocatorHashes = hashes.ToArray() };
      }

      /// <summary>
      /// Gets the current tip header node.
      /// </summary>
      /// <returns></returns>
      public HeaderNode GetTip()
      {
         using (new ReadLock(this._theLock))
         {
            return this.GetHeaderNodeNoLock(this._height);
         }
      }

      /// <summary>
      /// Determines whether an header is present in the best chain.
      /// </summary>
      /// <param name="headerNode">The header node to check.</param>
      public bool IsInBestChain(HeaderNode? headerNode)
      {
         if (headerNode == null) return false;

         using (new ReadLock(this._theLock))
         {
            int headerHeight = headerNode.Height;
            return this._bestChain.Count > headerHeight && this._bestChain[headerHeight] == headerNode.Hash;
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private HeaderNode GetHeaderNodeNoLock(int height)
      {
         return this._knownHeaders[this._bestChain[height]];
      }

      /// <summary>
      /// Tries to get the height of an hash on the best chain (no lock).
      /// </summary>
      /// <param name="blockHash">The block hash.</param>
      /// <param name="height">The height.</param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private bool TryGetNodeOnBestChainNoLock(UInt256 blockHash, [MaybeNullWhen(false)] out HeaderNode node)
      {
         if (this._knownHeaders.TryGetValue(blockHash, out node!) && this._height > node.Height && this._bestChain[node.Height] == blockHash)
         {
            return true;
         }
         else
         {
            node = null!;
            return false;
         }
      }
   }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;
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
   public class HeadersTree
   {
      private const int INITIAL_ITEMS_ALLOCATED = 16 ^ 2; //this parameter may go into settings, better to be multiple of 2

      private readonly ReaderWriterLockSlim theLock = new ReaderWriterLockSlim();

      private readonly ILogger<HeadersTree> logger;
      private readonly INetworkDefinition chainDefinition;
      readonly IBlockHeaderRepository blockHeaderRepository;
      readonly IConsensusValidator consensusValidator;

      /// <summary>
      /// Known set of hashes, both on forks and on best chains.
      /// Those who are on the best chain, can be found in the <see cref="bestChain"/> list.
      /// </summary>
      private readonly Dictionary<UInt256, HeaderNode> knownHeaders = new Dictionary<UInt256, HeaderNode>(INITIAL_ITEMS_ALLOCATED);

      /// <summary>
      /// The best chain of hashes sorted by height.
      /// If a block hash is in this list, it means it's in the main chain and can be sent to other peers.
      /// </summary>
      private readonly List<UInt256> bestChain = new List<UInt256>(INITIAL_ITEMS_ALLOCATED);

      /// <summary>
      /// The genesis node.
      /// </summary>
      private readonly HeaderNode genesisNode;

      public UInt256 Genesis => this.chainDefinition.Genesis;

      private int height;
      public int Height => this.height;

      public HeadersTree(ILogger<HeadersTree> logger, INetworkDefinition chainDefinition, IBlockHeaderRepository blockHeaderRepository, IConsensusValidator consensusValidator)
      {
         this.logger = logger;
         this.chainDefinition = chainDefinition ?? throw new ArgumentNullException(nameof(chainDefinition));
         this.blockHeaderRepository = blockHeaderRepository;
         this.consensusValidator = consensusValidator;

         this.genesisNode = new HeaderNode(0, this.chainDefinition.Genesis, null, Target.Zero);

         this.ResetToGenesis();
      }

      private void ResetToGenesis()
      {
         using (new WriteLock(this.theLock))
         {
            this.height = 0;
            this.bestChain.Clear();
            this.knownHeaders.Clear();

            this.bestChain.Add(this.chainDefinition.Genesis);
            this.knownHeaders.Add(this.chainDefinition.Genesis, this.genesisNode);
         }
      }

      public bool Contains(UInt256 blockHash)
      {
         using (new ReadLock(this.theLock))
         {
            return this.knownHeaders.ContainsKey(blockHash);
         }
      }

      /// <summary>
      /// Tries to get the height of an hash.
      /// </summary>
      /// <param name="blockHash">The block hash.</param>
      /// <param name="onlyBestChain">if set to <c>true</c> check only headers that belong to the best chain.</param>
      /// <param name="height">The height if found, -1 otherwise.</param>
      /// <returns><c>true</c> if the result has been found, <see langword="false"/> otherwise.</returns>
      public bool TryGetNode(UInt256 blockHash, bool onlyBestChain, [MaybeNullWhen(false)] out HeaderNode node)
      {
         using (new ReadLock(this.theLock))
         {
            return onlyBestChain ? this.knownHeaders.TryGetValue(blockHash, out node!) : this.TryGetNodeOnBestChainNoLock(blockHash, out node!);
         }
      }

      /// <summary>
      /// Tries to get the node on best chain at a specified height.
      /// </summary>
      /// <param name="height">The height.</param>
      /// <returns></returns>
      public bool TryGetNodeOnBestChain(int height, [MaybeNullWhen(false)] out HeaderNode node)
      {
         using (new ReadLock(this.theLock))
         {
            if (height > this.height)
            {
               node = null!;
               return false;
            }

            node = this.GetHeaderNodeNoLock(height);
            return true;
         }
      }

      /// <summary>
      /// Tries the get hash of a block at the specified height.
      /// </summary>
      /// <param name="height">The height.</param>
      /// <param name="blockHash">The block hash.</param>
      /// <returns></returns>
      public bool TryGetHash(int height, [MaybeNullWhen(false)]out UInt256 blockHash)
      {
         using (new ReadLock(this.theLock))
         {
            if (height > this.height || height < 0)
            {
               blockHash = null!;
               return false;
            }

            blockHash = this.bestChain[height];
         }

         return true;
      }

      /// <summary>
      /// Set a new tip in the chain
      /// </summary>
      /// <param name="newTip">The new tip</param>
      /// <param name="newTipPreviousHash">The block hash before the new tip</param>
      public ConnectHeaderResult TrySetTip(in BlockHeader newTip, ref BlockValidationState validationState)
      {
         UInt256 newTipHash = newTip.Hash!;
         UInt256? newTipPreviousHash = newTip.PreviousBlockHash;

         using (new WriteLock(this.theLock))
         {
            if (newTipHash == this.Genesis)
            {
               if (newTipPreviousHash != null)
               {
                  validationState.Invalid(BlockValidationFailureContext.BlockInvalidHeader, "invalid genesis", "genesis block should not have previous block.");
                  return this.ValidationFailure(validationState);
               }

               this.ResetToGenesis();

               return ConnectHeaderResult.ResettedToGenesis;
            }
            else
            {
               if (newTipPreviousHash == null)
               {
                  validationState.Invalid(BlockValidationFailureContext.BlockInvalidHeader, "null previous header", "previous hash null allowed only on genesis block");
                  return this.ValidationFailure(validationState);
               }
            }

            // check if the tip we want to set is already into our chain
            if (this.knownHeaders.TryGetValue(newTipHash, out HeaderNode? tipNode))
            {
               if (tipNode.Validity.HasFlag(HeaderValidityStatuses.FailedMask))
               {
                  validationState.Invalid(BlockValidationFailureContext.BlockCachedInvalid, "duplicate", "block marked as invalid");
                  return this.ValidationFailure(validationState);
               }

               if (!this.checkProofOfWorkRule.Check(new HeaderValidationContext(newTip)))
               {
                  validationState.Invalid(BlockValidationFailureContext.BlockInvalidHeader, "high-hash", "invalid proof of work");
                  return this.ValidationFailure(validationState);
               }

               this.logger.LogDebug("The tip we want to set is already in our headers chain.");
               return ConnectHeaderResult.Connected;
            }

            // ensures tip previous header is present.
            if (!this.knownHeaders.TryGetValue(newTipPreviousHash, out HeaderNode? newTipPreviousHeader))
            {
               //previous tip header not found, abort.
               validationState.Invalid(BlockValidationFailureContext.BlockMissingPreviousHeader, "prev-blk-not-found", "previous header not found, can't connect headers");
               return this.ValidationFailure(validationState);
            }

            if (newTipPreviousHeader.Validity.HasFlag(HeaderValidityStatuses.FailedMask))
            {
               validationState.Invalid(BlockValidationFailureContext.BlockCachedInvalid, "bad-prevblk", "previous block invalid");
               return this.ValidationFailure(validationState);
            }

            continue L3612 validation.cpp

            // if newTipPreviousHash isn't current tip, means we need to rollback
            bool needRewind = this.height != newTipPreviousHeader.Height;
            if (needRewind)
            {
               int rollingBackHeight = this.height;
               while (rollingBackHeight > newTipPreviousHeader.Height)
               {
                  this.knownHeaders.Remove(this.bestChain[rollingBackHeight]);
                  this.bestChain.RemoveAt(rollingBackHeight);
                  rollingBackHeight--;
               }
               this.height = rollingBackHeight;
            }

            // now we can put the tip on top of our chain.
            this.height++;
            this.bestChain.Add(newTipHash);
            this.knownHeaders.Add(newTipHash, new HeaderNode(this.height, newTipHash, newTipPreviousHash));
            this.blockHeaderRepository.TryAdd(newTip);

            return needRewind ? ConnectHeaderResult.Rewinded : ConnectHeaderResult.Connected;
         }
      }

      public BlockLocator GetTipLocator()
      {
         using (new ReadLock(this.theLock))
         {
            return this.GetLocatorNoLock(this.height);
         }
      }


      /// <summary>
      /// Gets the full block header tip.
      /// </summary>
      /// <returns></returns>
      public BlockHeader GetTipAsBlockHeader()
      {
         using (new ReadLock(this.theLock))
         {
            if (!this.blockHeaderRepository.TryGet(this.bestChain[this.height], out BlockHeader? header))
            {
               ThrowHelper.ThrowBlockHeaderRepositoryException($"Unexpected error, cannot fetch the tip at height {this.height}.");
            }

            return header!;
         }
      }

      public BlockLocator? GetLocator(int height)
      {
         using (new ReadLock(this.theLock))
         {
            return (height > this.height || height < 0) ? null : this.GetLocatorNoLock(height);
         }
      }

      public BlockLocator? GetLocator(UInt256 blockHash)
      {
         using (new ReadLock(this.theLock))
         {
            return (!this.knownHeaders.TryGetValue(blockHash, out HeaderNode? node)) ? null : this.GetLocatorNoLock(node.Height);
         }
      }

      /// <summary>
      /// Performing code to generate a <see cref="BlockLocator"/>.
      /// </summary>
      /// <param name="height">The height block locator starts from.</param>
      /// <returns></returns>
      private BlockLocator GetLocatorNoLock(int height)
      {
         int itemsToAdd = height <= 10 ? (height + 1) : (10 + (int)Math.Ceiling(Math.Log2(height)));
         UInt256[] hashes = new UInt256[itemsToAdd];

         int index = 0;
         while (index < 10 && height > 0)
         {
            hashes[index++] = this.bestChain[height--];
         }

         int step = 1;
         while (height > 0)
         {
            hashes[index++] = this.bestChain[height];
            step *= 2;
            height -= step;
         }
         hashes[itemsToAdd - 1] = this.Genesis;

         return new BlockLocator { BlockLocatorHashes = hashes };
      }

      /// <summary>
      /// Returns the first common block between our known best chain and the block locator.
      /// </summary>
      /// <param name="hashes">Hash to search for</param>
      /// <returns>First found block or genesis</returns>
      public HeaderNode GetHighestNodeInBestChainFromBlockLocator(BlockLocator blockLocator)
      {
         if (blockLocator == null) throw new ArgumentNullException(nameof(blockLocator));

         using (new ReadLock(this.theLock))
         {
            foreach (UInt256 hash in blockLocator.BlockLocatorHashes)
            {
               // ensure that any header we have in common belong to the main chain.
               if (this.TryGetNodeOnBestChainNoLock(hash, out HeaderNode? node))
               {
                  return node;
               }
            }
         }

         return this.genesisNode;
      }

      /// <summary>
      /// Gets the current tip header node.
      /// </summary>
      /// <returns></returns>
      public HeaderNode GetTip()
      {
         using (new ReadLock(this.theLock))
         {
            return this.GetHeaderNodeNoLock(this.height);
         }
      }

      public bool TryAddHeaders(BlockHeader[] headers, out BlockValidationState state, [MaybeNullWhen(false)]out HeaderNode lastProcessedHeader)
      {
         lastProcessedHeader = null;

         using (new WriteLock(this.theLock))
         {
            foreach (BlockHeader header in headers)
            {
               bool accepted = this.consensusValidator.ValidateHeader(header, out state);
               this.consensusValidator.CheckBlockIndex();

               if (!accepted) return false;

               lastProcessedHeader = header;
            }
         }
      }

      public bool IsInBestChain(HeaderNode? headerNode)
      {
         if (headerNode == null) return false;

         using (new ReadLock(this.theLock))
         {
            int headerHeight = headerNode.Height;
            return this.bestChain.Count < headerHeight && this.bestChain[headerHeight] == headerNode.Hash;
         }
      }

      /// <summary>
      /// Determines whether the specified hash is a known hash.
      /// May be present on best chain or on a fork.
      /// </summary>
      /// <param name="hash">The hash.</param>
      /// <returns>
      ///   <c>true</c> if the specified hash is known; otherwise, <c>false</c>.
      /// </returns>
      public bool IsKnown(UInt256? hash)
      {
         if (hash == null) return false;

         using (new ReadLock(this.theLock))
         {
            return this.knownHeaders.ContainsKey(hash);
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private HeaderNode GetHeaderNodeNoLock(int height)
      {
         return this.knownHeaders[this.bestChain[height]];
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
         if (this.knownHeaders.TryGetValue(blockHash, out node!) && this.height > node.Height && this.bestChain[node.Height] == blockHash)
         {
            return true;
         }
         else
         {
            node = null!;
            return false;
         }
      }

      /// <summary>
      /// Logs the validation failure reason and return a <see cref="ConnectHeaderResult.Invalid"/>.
      /// </summary>
      /// <param name="validationState">Validation state containing failing reason.</param>
      /// <returns></returns>
      private ConnectHeaderResult ValidationFailure(BlockValidationState validationState)
      {
         this.logger.LogDebug("Header validation failure: {0}", validationState.ToString());
         return ConnectHeaderResult.Invalid;
      }
   }
}

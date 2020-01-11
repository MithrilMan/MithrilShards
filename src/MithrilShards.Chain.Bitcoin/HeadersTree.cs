using System;
using System.Collections.Generic;
using System.Threading;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin
{
   /// <summary>
   /// A thread safe headers lookup that tracks current chain.
   /// Internally it uses <see cref="ReaderWriterLockSlim"/> to ensure thread safety on every get and set method.
   /// </summary>
   public class HeadersLookup
   {
      private const int INITIAL_ITEMS_ALLOCATED = 16 ^ 2; //this parameter may go into settings, better to be multiple of 2

      private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
      private readonly Dictionary<UInt256, int> hashesHeight = new Dictionary<UInt256, int>(INITIAL_ITEMS_ALLOCATED);
      private readonly IChainDefinition chainDefinition;
      private UInt256[] hashesByHeight = new UInt256[INITIAL_ITEMS_ALLOCATED];

      public UInt256 Genesis => this.chainDefinition.Genesis;

      private int height;
      public int Height => this.height;

      public HeadersLookup(IChainDefinition chainDefinition)
      {
         this.chainDefinition = chainDefinition ?? throw new ArgumentNullException(nameof(chainDefinition));

         this.hashesByHeight[0] = chainDefinition.Genesis;
         this.hashesHeight.Add(chainDefinition.Genesis, 0);
         this.height = 0;
      }


      public bool Contains(UInt256 blockHash)
      {
         using (new ReadLock(this._lock))
         {
            return this.hashesHeight.ContainsKey(blockHash);
         }
      }

      public bool TryGetHeight(UInt256 blockHash, out int height)
      {
         using (new ReadLock(this._lock))
         {
            return this.hashesHeight.TryGetValue(blockHash, out height);
         }
      }

      public bool TryGetHash(int height, out UInt256 blockHash)
      {
         using (new ReadLock(this._lock))
         {
            if (height > this.height || height < 0)
            {
               blockHash = default(UInt256);
               return false;
            }
            blockHash = this.hashesByHeight[height];
         }

         return true;
      }

      public void ResetToGenesis()
      {
         this.TrySetTip(this.Genesis, null);
      }

      /// <summary>
      /// Set a new tip in the chain
      /// </summary>
      /// <param name="newTip">The new tip</param>
      /// <param name="previous">The block hash before the new tip</param>
      /// <param name="nopIfContainsTip">If true and the new tip is already included somewhere in the chain, do nothing</param>
      /// <returns>True if newTip is the new tip</returns>
      public bool TrySetTip(UInt256 newTip, UInt256 previous, bool nopIfContainsTip = false)
      {
         using (new WriteLock(this._lock))
         {
            return this.TrySetTipNoLock(newTip, previous, nopIfContainsTip);
         }
      }

      private bool TrySetTipNoLock(in UInt256 newTip, in UInt256 previous, bool nopIfContainsTip)
      {
         if (newTip == null)
            throw new ArgumentNullException(nameof(newTip));

         if (newTip == previous)
            throw new ArgumentException(message: "newTip should be different from previous");

         if (newTip == this.hashesByHeight[this.height])
         {
            if (newTip != this.hashesByHeight[0] && this.hashesByHeight[this.height - 1] != previous)
               throw new ArgumentException(message: "newTip is already inserted with a different previous block, this should never happen");

            return true;
         }

         if (this.hashesHeight.TryGetValue(newTip, out int newTipHeight))
         {
            if (newTipHeight - 1 >= 0 && this.hashesByHeight[newTipHeight - 1] != previous)
               throw new ArgumentException(message: "newTip is already inserted with a different previous block, this should never happen");

            if (newTipHeight == 0 && this.hashesByHeight[0] != newTip)
               throw new InvalidOperationException("Unexpected genesis block");

            if (newTipHeight == 0 && previous != null)
               throw new ArgumentException(message: "Genesis block should not have previous block", paramName: nameof(previous));

            if (nopIfContainsTip) return false;
         }

         if (previous == null && newTip != this.hashesByHeight[0]) throw new InvalidOperationException("Unexpected genesis block");

         int prevHeight = -1;
         if (previous != null && !this.hashesHeight.TryGetValue(previous, out prevHeight))
            return false;

         for (int i = this.height; i > prevHeight; i--)
         {
            this.hashesHeight.Remove(this.hashesByHeight[i]);
            this.hashesByHeight[i] = null;
         }

         this.height = prevHeight + 1;
         if (this.hashesByHeight.Length <= this.height)
            Array.Resize(ref this.hashesByHeight, (int)((this.height + 100) * 1.1));
         this.hashesByHeight[this.height] = newTip;
         this.hashesHeight.Add(newTip, this.height);

         return true;
      }

      public BlockLocator GetTipLocator()
      {
         using (new ReadLock(this._lock))
         {
            return this.GetLocatorNoLock(this.height);
         }
      }

      public BlockLocator GetLocator(int height)
      {
         using (new ReadLock(this._lock))
         {
            if (height > this.height || height < 0)
               return null;
            return this.GetLocatorNoLock(height);
         }
      }

      public BlockLocator GetLocator(UInt256 blockHash)
      {
         using (new ReadLock(this._lock))
         {
            if (!this.hashesHeight.TryGetValue(blockHash, out int height))
               return null;
            return this.GetLocatorNoLock(height);
         }
      }

      private BlockLocator GetLocatorNoLock(int height)
      {

         int itemsToAdd = height <= 10 ? (height + 1) : (10 + (int)Math.Ceiling(Math.Log2(height)));
         UInt256[] hashes = new UInt256[itemsToAdd];

         int index = 0;
         while (index < 10 && height > 0)
         {
            hashes[index++] = this.hashesByHeight[height--];
         }

         int step = 1;
         while (height > 0)
         {
            hashes[index++] = this.hashesByHeight[height];
            step *= 2;
            height -= step;
         }
         hashes[itemsToAdd - 1] = this.Genesis;

         return new BlockLocator { BlockLocatorHashes = hashes };
      }

      ///// <summary>
      ///// Returns the first found block
      ///// </summary>
      ///// <param name="hashes">Hash to search for</param>
      ///// <returns>First found block or null</returns>
      //public SlimChainedBlock FindFork(BlockLocator blockLocator)
      //{
      //   if (blockLocator == null)
      //      throw new ArgumentNullException(nameof(blockLocator));
      //   // Find the first block the caller has in the main chain
      //   foreach (UInt256 hash in blockLocator.BlockLocatorHashes)
      //   {
      //      if (this._HeightsByBlockHash.TryGetValue(hash, out int height))
      //      {
      //         return this.CreateSlimBlock(height);
      //      }
      //   }
      //   return null;
      //}

      public UInt256 Tip
      {
         get
         {
            using (new ReadLock(this._lock))
            {
               return this.hashesByHeight[this.height];
            }
         }
      }

      //public SlimChainedBlock TipBlock
      //{
      //   get
      //   {
      //      using (this._lock.LockRead())
      //      {
      //         return CreateSlimBlock(this.Height);
      //      }
      //   }
      //}

      //public SlimChainedBlock GetBlock(int height)
      //{
      //   using (this._lock.LockRead())
      //   {
      //      if (height > this.Height || height < 0)
      //         return null;
      //      return this.CreateSlimBlock(height);
      //   }
      //}

      //public SlimChainedBlock GetBlock(UInt256 blockHash)
      //{
      //   using (_lock.LockRead())
      //   {
      //      if (!_HeightsByBlockHash.TryGetValue(blockHash, out int height))
      //         return null;
      //      return CreateSlimBlock(height);
      //   }
      //}

      //private SlimChainedBlock CreateSlimBlock(int height)
      //{
      //   return new SlimChainedBlock(_BlockHashesByHeight[height], height == 0 ? null : _BlockHashesByHeight[height - 1], height);
      //}


      public override string ToString()
      {
         using (new ReadLock(this._lock))
         {
            return $"Height: {this.Height}, Hash: {this.hashesByHeight[this.height]}";
         }
      }
   }
}

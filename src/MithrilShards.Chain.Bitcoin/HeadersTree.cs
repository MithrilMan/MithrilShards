using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin
{
   //public class HeadersTree
   //{
   //   Dictionary<int, UInt256> hashesByHeight = new Dictionary<int, UInt256>();

   //   public readonly int Test(Dictionary<int, UInt256> a)
   //   {
   //      return this.hashesByHeight.Count + a.Count;
   //   }
   //}


   /// <summary>
   /// A thread safe, memory optimized chain of hashes representing the current chain
   /// </summary>
   public class HeadersLookup
   {
      readonly Dictionary<UInt256, int> _HeightsByBlockHash = new Dictionary<UInt256, int>();
      UInt256[] _BlockHashesByHeight = new UInt256[1];
      int height;
      private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

      public HeadersLookup(UInt256 genesis)
      {
         this._BlockHashesByHeight[0] = genesis;
         this._HeightsByBlockHash.Add(genesis, 0);
         this.height = 0;
      }

      public int Height => this.height;

      public bool Contains(UInt256 blockHash)
      {
         this._lock.EnterReadLock();
         try
         {
            return this._HeightsByBlockHash.ContainsKey(blockHash);
         }
         finally
         {
            this._lock.ExitReadLock();
         }
      }

      public bool TryGetHeight(UInt256 blockHash, out int height)
      {
         this._lock.EnterReadLock();
         try
         {
            return this._HeightsByBlockHash.TryGetValue(blockHash, out height);
         }
         finally
         {
            this._lock.ExitReadLock();
         }
      }

      public bool TryGetHash(int height, out UInt256 blockHash)
      {
         this._lock.EnterReadLock();
         try
         {
            if (height > this.height || height < 0)
            {
               blockHash = default(UInt256);
               return false;
            }
            blockHash = this._BlockHashesByHeight[height];
         }
         finally
         {
            this._lock.ExitReadLock();
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
         this._lock.EnterWriteLock();
         try
         {
            return this.TrySetTipNoLock(newTip, previous, nopIfContainsTip);
         }
         finally
         {
            this._lock.ExitWriteLock();
         }
      }

      private bool TrySetTipNoLock(in UInt256 newTip, in UInt256 previous, bool nopIfContainsTip)
      {
         if (newTip == null)
            throw new ArgumentNullException(nameof(newTip));
         if (newTip == previous)
            throw new ArgumentException(message: "newTip should be different from previous");

         if (newTip == this._BlockHashesByHeight[this.height])
         {
            if (newTip != this._BlockHashesByHeight[0] && this._BlockHashesByHeight[this.height - 1] != previous)
               throw new ArgumentException(message: "newTip is already inserted with a different previous block, this should never happen");
            return true;
         }

         if (this._HeightsByBlockHash.TryGetValue(newTip, out int newTipHeight))
         {
            if (newTipHeight - 1 >= 0 && this._BlockHashesByHeight[newTipHeight - 1] != previous)
               throw new ArgumentException(message: "newTip is already inserted with a different previous block, this should never happen");

            if (newTipHeight == 0 && this._BlockHashesByHeight[0] != newTip)
            {
               throw new InvalidOperationException("Unexpected genesis block");
            }

            if (newTipHeight == 0 && previous != null)
               throw new ArgumentException(message: "Genesis block should not have previous block", paramName: nameof(previous));

            if (nopIfContainsTip)
               return false;
         }

         if (previous == null && newTip != this._BlockHashesByHeight[0])
            throw new InvalidOperationException("Unexpected genesis block");

         int prevHeight = -1;
         if (previous != null && !this._HeightsByBlockHash.TryGetValue(previous, out prevHeight))
            return false;
         for (int i = this.height; i > prevHeight; i--)
         {
            this._HeightsByBlockHash.Remove(this._BlockHashesByHeight[i]);
            this._BlockHashesByHeight[i] = null;
         }
         this.height = prevHeight + 1;
         if (this._BlockHashesByHeight.Length <= this.height)
            Array.Resize(ref this._BlockHashesByHeight, (int)((this.height + 100) * 1.1));
         this._BlockHashesByHeight[this.height] = newTip;
         this._HeightsByBlockHash.Add(newTip, this.height);
         return true;
      }

      public BlockLocator GetTipLocator()
      {
         this._lock.EnterReadLock();
         try
         {
            return this.GetLocatorNoLock(this.height);
         }
         finally
         {
            this._lock.ExitReadLock();
         }
      }

      public BlockLocator GetLocator(int height)
      {
         this._lock.EnterReadLock();
         try
         {
            if (height > this.height || height < 0)
               return null;
            return this.GetLocatorNoLock(height);
         }
         finally
         {
            this._lock.ExitReadLock();
         }
      }

      public BlockLocator GetLocator(UInt256 blockHash)
      {
         this._lock.EnterReadLock();
         try
         {
            if (!this._HeightsByBlockHash.TryGetValue(blockHash, out int height))
               return null;
            return this.GetLocatorNoLock(height);
         }
         finally
         {
            this._lock.ExitReadLock();
         }
      }

      private BlockLocator GetLocatorNoLock(int height)
      {
         int nStep = 1;
         var vHave = new List<UInt256>();
         while (true)
         {
            vHave.Add(this._BlockHashesByHeight[height]);
            // Stop when we have added the genesis block.
            if (height == 0)
               break;
            // Exponentially larger steps back, plus the genesis block.
            height = Math.Max(height - nStep, 0);
            if (vHave.Count > 10)
               nStep *= 2;
         }

         var locators = new BlockLocator();
         locators.BlockLocatorHashes = vHave.ToArray();
         return locators;
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
            this._lock.EnterReadLock();
            try
            {
               return this._BlockHashesByHeight[this.height];
            }
            finally
            {
               this._lock.ExitReadLock();
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

      public UInt256 Genesis
      {
         get
         {
            this._lock.EnterReadLock();
            try
            {
               return this._BlockHashesByHeight[0];
            }
            finally
            {
               this._lock.ExitReadLock();
            }
         }
      }

      public override string ToString()
      {
         this._lock.EnterReadLock();
         try
         {
            return $"Height: {this.Height}, Hash: {this._BlockHashesByHeight[this.height]}";
         }
         finally
         {
            this._lock.ExitReadLock();
         }
      }
   }
}

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
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

      private readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();
      private readonly Dictionary<UInt256, int> hashesHeight = new Dictionary<UInt256, int>(INITIAL_ITEMS_ALLOCATED);
      private readonly ILogger<HeadersLookup> logger;
      private readonly IChainDefinition chainDefinition;
      private readonly List<UInt256> hashesByHeight = new List<UInt256>(INITIAL_ITEMS_ALLOCATED);

      public UInt256 Genesis => this.chainDefinition.Genesis;

      private int height;
      public int Height => this.height;

      public UInt256 Tip
      {
         get
         {
            using (new ReadLock(this.@lock)) return this.hashesByHeight[this.height];
         }
      }

      public HeadersLookup(ILogger<HeadersLookup> logger, IChainDefinition chainDefinition)
      {
         this.logger = logger;
         this.chainDefinition = chainDefinition ?? throw new ArgumentNullException(nameof(chainDefinition));

         this.ResetToGenesis();
      }

      private void ResetToGenesis()
      {
         using (new WriteLock(this.@lock))
         {
            this.height = 0;
            this.hashesByHeight.Clear();
            this.hashesHeight.Clear();

            this.hashesByHeight.Add(this.chainDefinition.Genesis);
            this.hashesHeight.Add(this.chainDefinition.Genesis, 0);
         }
      }

      public bool Contains(UInt256 blockHash)
      {
         using (new ReadLock(this.@lock))
         {
            return this.hashesHeight.ContainsKey(blockHash);
         }
      }

      public bool TryGetHeight(UInt256 blockHash, out int height)
      {
         using (new ReadLock(this.@lock))
         {
            return this.hashesHeight.TryGetValue(blockHash, out height);
         }
      }

      public bool TryGetHash(int height, out UInt256 blockHash)
      {
         using (new ReadLock(this.@lock))
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

      /// <summary>
      /// Set a new tip in the chain
      /// </summary>
      /// <param name="newTip">The new tip</param>
      /// <param name="newTipPreviousHash">The block hash before the new tip</param>
      public ConnectHeaderResult TrySetTip(in UInt256 newTip, in UInt256 newTipPreviousHash)
      {
         using (new WriteLock(this.@lock))
         {
            return this.TrySetTipNoLock(newTip, newTipPreviousHash);
         }
      }

      private ConnectHeaderResult TrySetTipNoLock(in UInt256 newTip, in UInt256 newTipPreviousHash)
      {
         if (newTip == null) throw new ArgumentNullException(nameof(newTip));

         if (newTip == this.Genesis)
         {
            if (newTipPreviousHash != null)
            {
               throw new ArgumentException("Genesis block should not have previous block", nameof(newTipPreviousHash));
            }

            this.ResetToGenesis();

            return ConnectHeaderResult.ResettedToGenesis;
         }

         // check if the tip we want to set is already into our chain
         if (this.hashesHeight.TryGetValue(newTip, out int newTipHeight))
         {
            if (this.hashesByHeight[newTipHeight - 1] != newTipPreviousHash)
            {
               throw new ArgumentException("The new tip is already inserted with a different previous block.");
            }

            this.logger.LogDebug("The tip we want to set is already in our headers chain.");
         }

         // ensures tip previous header is present.
         if (!this.hashesHeight.TryGetValue(newTipPreviousHash, out int newTipPreviousHeaderHeight))
         {
            //previous tip header not found, abort.
            this.logger.LogDebug("New Tip previous header not found, can't connect headers.");
            return ConnectHeaderResult.MissingPreviousHeader;
         }

         // if newTipPreviousHash isn't current tip, means we need to rollback
         bool needRewind = this.height != newTipPreviousHeaderHeight;
         if (needRewind)
         {
            int rollingBackHeight = this.height;
            while (rollingBackHeight > newTipPreviousHeaderHeight)
            {
               this.hashesHeight.Remove(this.hashesByHeight[rollingBackHeight]);
               this.hashesByHeight[rollingBackHeight] = null;
               rollingBackHeight--;
            }
            this.height = rollingBackHeight;
         }

         // now we can put the tip on top of our chain.
         this.height++;
         this.hashesByHeight[this.height] = newTip;
         this.hashesHeight.Add(newTip, this.height);

         return needRewind ? ConnectHeaderResult.Rewinded : ConnectHeaderResult.Connected;
      }

      public BlockLocator GetTipLocator()
      {
         using (new ReadLock(this.@lock))
         {
            return this.GetLocatorNoLock(this.height);
         }
      }

      public BlockLocator GetLocator(int height)
      {
         using (new ReadLock(this.@lock))
         {
            if (height > this.height || height < 0)
               return null;
            return this.GetLocatorNoLock(height);
         }
      }

      public BlockLocator GetLocator(UInt256 blockHash)
      {
         using (new ReadLock(this.@lock))
         {
            if (!this.hashesHeight.TryGetValue(blockHash, out int height))
               return null;
            return this.GetLocatorNoLock(height);
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


      public HeaderNode GetTipHeaderNode()
      {
         using (new ReadLock(this.@lock))
         {
            int height = this.height;
            return this.GetHeaderNodeNoLock(height);
         }
      }

      public HeaderNode GetHeaderNode(int height)
      {
         using (new ReadLock(this.@lock))
         {
            return this.GetHeaderNodeNoLock(height);
         }
      }

      private HeaderNode GetHeaderNodeNoLock(int height)
      {
         return new HeaderNode(height, this.hashesByHeight[height], height == 0 ? null : this.hashesByHeight[height - 1]);
      }

      private HeaderNode GetHeaderNodeNoLock(int height, UInt256 currentHeader)
      {
         return new HeaderNode(height, currentHeader, height == 0 ? null : this.hashesByHeight[height - 1]);
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
}

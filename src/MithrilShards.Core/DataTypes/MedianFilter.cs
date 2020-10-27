using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MithrilShards.Core.Threading;

namespace MithrilShards.Core.DataTypes
{
   public class MedianFilter<T> where T : struct
   {
      ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();
      private readonly Queue<T> items;
      private readonly Func<(T lowerItem, T higherItem), T> medianComputationOnEvenElements;
      private readonly uint size;

      public int Count => this.items.Count;

      public MedianFilter(uint size, T initialValue, Func<(T lowerItem, T higherItem), T> medianComputationOnEvenElements)
      {
         this.size = size;
         this.medianComputationOnEvenElements = medianComputationOnEvenElements;
         items = new Queue<T>((int)size);
         items.Enqueue(initialValue);
      }

      public T GetMedian()
      {
         using var readLock = new ReadLock(lockSlim);
         if (items.Count <= 0)
         {
            ThrowHelper.ThrowArgumentException($"{nameof(size)} <= 0");
         }

         T[] sortedItems = items.OrderBy(o => o).ToArray();

         if (size % 2 == 1)
         {
            return sortedItems[size / 2];
         }
         else
         {
            return this.medianComputationOnEvenElements((sortedItems[size / 2 - 1], sortedItems[size / 2]));
         }
      }

      public void AddSample(T value)
      {
         using var writeLock = new WriteLock(lockSlim);
         if (items.Count == size)
         {
            items.Dequeue();
         }
         items.Enqueue(value);
      }
   }
}
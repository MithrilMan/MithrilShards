using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MithrilShards.Core.Threading;

namespace MithrilShards.Core.DataTypes;

public class MedianFilter<T> where T : struct
{
   readonly ReaderWriterLockSlim _lockSlim = new();
   private readonly Queue<T> _items;
   private readonly Func<(T lowerItem, T higherItem), T> _medianComputationOnEvenElements;
   private readonly uint _size;

   public int Count => _items.Count;

   public MedianFilter(uint size, T initialValue, Func<(T lowerItem, T higherItem), T> medianComputationOnEvenElements)
   {
      _size = size;
      _medianComputationOnEvenElements = medianComputationOnEvenElements;
      _items = new Queue<T>((int)size);
      _items.Enqueue(initialValue);
   }

   public T GetMedian()
   {
      using var readLock = new ReadLock(_lockSlim);
      if (_items.Count <= 0)
      {
         ThrowHelper.ThrowArgumentException($"{nameof(_size)} <= 0");
      }

      T[] sortedItems = _items.OrderBy(o => o).ToArray();

      if (_size % 2 == 1)
      {
         return sortedItems[_size / 2];
      }
      else
      {
         return _medianComputationOnEvenElements((sortedItems[_size / 2 - 1], sortedItems[_size / 2]));
      }
   }

   public void AddSample(T value)
   {
      using var writeLock = new WriteLock(_lockSlim);
      if (_items.Count == _size)
      {
         _items.Dequeue();
      }
      _items.Enqueue(value);
   }
}

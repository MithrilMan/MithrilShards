// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;

namespace MithrilShards.Core.Memory
{
   /// <summary>
   /// IBufferWriter implementation that uses ArrayPool to not having to allocate memory for short living data.
   /// Performance wise (speed), ArrayPool is worth to be used when dealing with big arrays.
   /// The article https://adamsitnik.com/Array-Pool/ shows some benchmark showing that when the size is 1kb speed is comparable to allocating implementation.
   /// After that point ArrayPool start winning. The bigger the array, the faster it is compared to allocating implementation.
   /// Memory wise, of course, Pooled implementation never allocates (except if you exceed the amount of the rentable memory)
   /// </summary>
   /// <remarks>
   /// Adapted from https://github.com/dotnet/runtime/blob/cd1d02995b6313bdaf0f13452fc36e21e35d6f8f/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/PooledByteBufferWriter.cs
   /// </remarks>
   public sealed class PooledByteBufferWriter : IBufferWriter<byte>, IDisposable
   {
      private byte[]? _rentedBuffer;
      private int _index;

      private const int MinimumBufferSize = 256;

      public PooledByteBufferWriter(int initialCapacity)
      {
         Debug.Assert(initialCapacity > 0);

         _rentedBuffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
         _index = 0;
      }

      public ReadOnlyMemory<byte> WrittenMemory
      {
         get
         {
            Debug.Assert(_rentedBuffer != null);
            Debug.Assert(_index <= _rentedBuffer.Length);
            return _rentedBuffer.AsMemory(0, _index);
         }
      }

      public int WrittenCount
      {
         get
         {
            Debug.Assert(_rentedBuffer != null);
            return _index;
         }
      }

      public int Capacity
      {
         get
         {
            Debug.Assert(_rentedBuffer != null);
            return _rentedBuffer.Length;
         }
      }

      public int FreeCapacity
      {
         get
         {
            Debug.Assert(_rentedBuffer != null);
            return _rentedBuffer.Length - _index;
         }
      }

      public void Clear()
      {
         ClearHelper();
      }

      private void ClearHelper()
      {
         Debug.Assert(_rentedBuffer != null);
         Debug.Assert(_index <= _rentedBuffer.Length);

         _rentedBuffer.AsSpan(0, _index).Clear();
         _index = 0;
      }

      // Returns the rented buffer back to the pool
      public void Dispose()
      {
         if (_rentedBuffer == null)
         {
            return;
         }

         ClearHelper();
         ArrayPool<byte>.Shared.Return(_rentedBuffer);
         _rentedBuffer = null;
      }

      public void Advance(int count)
      {
         Debug.Assert(_rentedBuffer != null);
         Debug.Assert(count >= 0);
         Debug.Assert(_index <= _rentedBuffer.Length - count);

         _index += count;
      }

      public Memory<byte> GetMemory(int sizeHint = 0)
      {
         CheckAndResizeBuffer(sizeHint);
         return _rentedBuffer.AsMemory(_index);
      }

      public Span<byte> GetSpan(int sizeHint = 0)
      {
         CheckAndResizeBuffer(sizeHint);
         return _rentedBuffer.AsSpan(_index);
      }

//#if BUILDING_INBOX_LIBRARY
//        internal ValueTask WriteToStreamAsync(Stream destination, CancellationToken cancellationToken)
//        {
//            return destination.WriteAsync(WrittenMemory, cancellationToken);
//        }
//#else
//      internal Task WriteToStreamAsync(Stream destination, CancellationToken cancellationToken)
//      {
//         return destination.WriteAsync(_rentedBuffer!, 0, _index, cancellationToken);
//      }
//#endif

      private void CheckAndResizeBuffer(int sizeHint)
      {
         Debug.Assert(_rentedBuffer != null);
         Debug.Assert(sizeHint >= 0);

         if (sizeHint == 0)
         {
            sizeHint = MinimumBufferSize;
         }

         int availableSpace = _rentedBuffer.Length - _index;

         if (sizeHint > availableSpace)
         {
            int growBy = Math.Max(sizeHint, _rentedBuffer.Length);

            int newSize = checked(_rentedBuffer.Length + growBy);

            byte[] oldBuffer = _rentedBuffer;

            _rentedBuffer = ArrayPool<byte>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _index);
            Debug.Assert(_rentedBuffer.Length >= _index);

            Span<byte> previousBuffer = oldBuffer.AsSpan(0, _index);
            previousBuffer.CopyTo(_rentedBuffer);
            previousBuffer.Clear();
            ArrayPool<byte>.Shared.Return(oldBuffer);
         }

         Debug.Assert(_rentedBuffer.Length - _index > 0);
         Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
      }
   }
}
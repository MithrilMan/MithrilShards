using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace MithrilShards.Core {
   public class DefaultRandomNumberGenerator : IRandomNumberGenerator {
      private static readonly RandomNumberGenerator instance = RandomNumberGenerator.Create();

      public void GetBytes(Span<byte> data) {
         instance.GetBytes(data);
      }

      public void GetNonZeroBytes(Span<byte> data) {
         instance.GetNonZeroBytes(data);
      }

      public int GetInt32() {
         Span<int> resultSpan = stackalloc int[1];
         instance.GetBytes(MemoryMarshal.AsBytes(resultSpan));
         return resultSpan[0];
      }

      public uint GetUint32() {
         Span<uint> resultSpan = stackalloc uint[1];
         instance.GetBytes(MemoryMarshal.AsBytes(resultSpan));
         return resultSpan[0];
      }

      public long GetInt64() {
         Span<long> resultSpan = stackalloc long[1];
         instance.GetBytes(MemoryMarshal.AsBytes(resultSpan));
         return resultSpan[0];
      }

      public ulong GetUint64() {
         Span<ulong> resultSpan = stackalloc ulong[1];
         instance.GetBytes(MemoryMarshal.AsBytes(resultSpan));
         return resultSpan[0];
      }
   }
}

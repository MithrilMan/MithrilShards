﻿using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MithrilShards.Core
{
   public class DefaultRandomNumberGenerator : IRandomNumberGenerator
   {
      private static readonly RandomNumberGenerator _instance = RandomNumberGenerator.Create();

      public void GetBytes(Span<byte> data)
      {
         _instance.GetBytes(data);
      }

      public void GetNonZeroBytes(Span<byte> data)
      {
         _instance.GetNonZeroBytes(data);
      }

      public int GetInt32()
      {
         Span<int> resultSpan = stackalloc int[1];
         _instance.GetBytes(MemoryMarshal.AsBytes(resultSpan));
         return resultSpan[0];
      }

      public uint GetUint32()
      {
         Span<uint> resultSpan = stackalloc uint[1];
         _instance.GetBytes(MemoryMarshal.AsBytes(resultSpan));
         return resultSpan[0];
      }

      public long GetInt64()
      {
         Span<long> resultSpan = stackalloc long[1];
         _instance.GetBytes(MemoryMarshal.AsBytes(resultSpan));
         return resultSpan[0];
      }

      public ulong GetUint64()
      {
         Span<ulong> resultSpan = stackalloc ulong[1];
         _instance.GetBytes(MemoryMarshal.AsBytes(resultSpan));
         return resultSpan[0];
      }
   }
}

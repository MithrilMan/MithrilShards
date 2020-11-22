using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.DataTypes
{
   public partial class Target : UInt256
   {
      //public static Target operator +(Target left, Target right)
      //{
      //   const int UINT_ELEMENTS_COUNT = EXPECTED_SIZE / sizeof(uint);
      //   const int UINT_BIT_SIZE = sizeof(uint) * 8;
      //
      //   ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);
      //   ReadOnlySpan<uint> rightBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref right.part1), UINT_ELEMENTS_COUNT);

      //   Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
      //   leftBytes.CopyTo(result);

      //   ulong carry = 0;
      //   for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
      //   {
      //      ulong n = carry + result[i] + rightBytes[i];
      //      result[i] = (uint)(n & 0xffffffff);
      //      carry = n >> UINT_BIT_SIZE;
      //   }

      //   return new Target(MemoryMarshal.Cast<uint, byte>(result));
      //}

      public static Target operator +(Target left, Target right)
      {
         const int ELEMENTS = EXPECTED_SIZE / sizeof(uint);
         const int SIZE = sizeof(uint) * 8;

         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), ELEMENTS);
         ReadOnlySpan<uint> rightBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref right.part1), ELEMENTS);

         Span<uint> result = stackalloc uint[ELEMENTS];
         result.Clear();

         long carry = 0;
         for (int i = 0; i < ELEMENTS; i++)
         {
            long n = carry + leftBytes[i] + rightBytes[i];
            result[i] = (uint)(n & 0xffffffff);
            carry = n >> SIZE;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator ~(Target left)
      {
         return new Target
         {
            part1 = ~left.part1,
            part2 = ~left.part2,
            part3 = ~left.part3,
            part4 = ~left.part4,
         };
      }

      private void ShiftLeft(int shiftAmount)
      {
         const int ELEMENTS = EXPECTED_SIZE / sizeof(ulong);
         const int SIZE = sizeof(ulong) * 8;

         Span<ulong> data = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, ulong>(ref part1), ELEMENTS);

         Span<ulong> result = stackalloc ulong[ELEMENTS];
         result.Clear();

         int k = shiftAmount / SIZE;
         shiftAmount %= SIZE;

         for (int i = 0; i < ELEMENTS - k; i++)
         {
            if (i + k + 1 < ELEMENTS && shiftAmount != 0)
            {
               result[i + k + 1] |= data[i] >> (SIZE - shiftAmount);
            }

            result[i + k] |= data[i] << shiftAmount;
         }

         result.CopyTo(data);
      }

      private uint GetCompactMantissa(int shiftAmount)
      {
         const int ELEMENTS = EXPECTED_SIZE / sizeof(uint);
         const int SIZE = sizeof(uint) * 8;

         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref part1), ELEMENTS);

         int k = shiftAmount / SIZE;
         shiftAmount %= SIZE;

         Span<uint> result = stackalloc uint[ELEMENTS - k];
         result.Clear();

         for (int i = k; i < ELEMENTS; i++)
         {
            if (i - k - 1 >= 0 && shiftAmount != 0)
            {
               result[i - k - 1] |= leftBytes[i] << (SIZE - shiftAmount);
            }

            result[i - k] |= (leftBytes[i] >> shiftAmount);
         }

         return result[0];
      }

      public static Target operator >>(Target left, int shiftAmount)
      {
         const int ELEMENTS = EXPECTED_SIZE / sizeof(uint);
         const int SIZE = sizeof(uint) * 8;

         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), ELEMENTS);

         Span<uint> result = stackalloc uint[ELEMENTS];
         result.Clear();

         int k = shiftAmount / SIZE;
         shiftAmount %= SIZE;

         for (int i = k; i < ELEMENTS; i++)
         {
            if (i - k - 1 >= 0 && shiftAmount != 0)
            {
               result[i - k - 1] |= leftBytes[i] << (SIZE - shiftAmount);
            }

            result[i - k] |= (leftBytes[i] >> shiftAmount);
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public void Multiply(uint value)
      {
         const int ELEMENTS = EXPECTED_SIZE / sizeof(uint);
         const int SIZE = sizeof(uint) * 8;

         Span<uint> data = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, uint>(ref part1), ELEMENTS);

         ulong carry = 0;
         for (int i = 0; i < ELEMENTS; i++)
         {
            ulong n = carry + ((ulong)value * data[i]);
            data[i] = (uint)(n & 0xffffffff);
            carry = n >> SIZE;
         }

         //this is slower
         //BigInteger me = new BigInteger(this.GetBytes());
         //Span<byte> data = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref this.part1), EXPECTED_SIZE);
         //data.Clear();
         //(me * value).TryWriteBytes(data, out _);
      }

      public void Divide(uint divisor)
      {
         if (divisor == 0)
         {
            ThrowHelper.ThrowArgumentException("Division by zero");
         }

         var dividend = new BigInteger(GetBytes());

         Span<byte> data = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref part1), EXPECTED_SIZE);
         data.Clear();
         (dividend / divisor).TryWriteBytes(data, out _);
      }
   }
}
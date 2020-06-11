using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.DataTypes
{
   public partial class Target : UInt256
   {
      const int UINT_ELEMENTS_COUNT = EXPECTED_SIZE / sizeof(uint);
      const int UINT_BIT_SIZE = sizeof(uint) * 8;

      public static Target operator +(Target left, Target right)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);
         ReadOnlySpan<uint> rightBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref right.part1), UINT_ELEMENTS_COUNT);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         leftBytes.CopyTo(result);

         ulong carry = 0;
         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            ulong n = carry + result[i] + rightBytes[i];
            result[i] = (uint)(n & 0xffffffff);
            carry = n >> UINT_BIT_SIZE;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator -(Target item)
      {
         return new Target
         {
            part1 = ~item.part1,
            part2 = ~item.part2,
            part3 = ~item.part3,
            part4 = ~item.part4,
         };
      }

      public static Target operator -(Target left, Target right)
      {
         return left + -right;
      }

      public static Target operator <<(Target left, int shiftAmount)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         result.Fill(0);

         int k = shiftAmount / UINT_BIT_SIZE;
         shiftAmount %= UINT_BIT_SIZE;

         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            if (i + k + 1 < UINT_ELEMENTS_COUNT && shiftAmount != 0)
               result[i + k + 1] |= leftBytes[i] >> (UINT_BIT_SIZE - shiftAmount);

            if (i + k < UINT_ELEMENTS_COUNT)
               result[i + k] |= leftBytes[i] << shiftAmount;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator >>(Target left, int shiftAmount)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         result.Fill(0);

         int k = shiftAmount / UINT_BIT_SIZE;
         shiftAmount %= UINT_BIT_SIZE;

         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            if (i - k - 1 >= 0 && shiftAmount != 0)
               result[i - k - 1] |= leftBytes[i] << (UINT_BIT_SIZE - shiftAmount);

            if (i - k >= 0)
               result[i - k] |= (leftBytes[i] >> shiftAmount);
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator *(Target left, Target right)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);
         ReadOnlySpan<uint> rightBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref right.part1), UINT_ELEMENTS_COUNT);

         Target result = new Target();
         Span<uint> resultSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, uint>(ref result.part1), UINT_ELEMENTS_COUNT);

         for (int j = 0; j < UINT_ELEMENTS_COUNT; j++)
         {
            ulong carry = 0;
            for (int i = 0; i + j < UINT_ELEMENTS_COUNT; i++)
            {
               ulong n = carry + resultSpan[i + j] + ((ulong)leftBytes[j] * rightBytes[i]);
               resultSpan[i + j] = (uint)(n & 0xffffffff);
               carry = n >> UINT_BIT_SIZE;
            }
         }

         return result;
      }

      public static Target operator *(Target left, uint value)
      {
         Target result = new Target();
         Span<uint> resultSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, uint>(ref result.part1), UINT_ELEMENTS_COUNT);

         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);
         ulong carry = 0;
         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            ulong n = carry + ((ulong)value * leftBytes[i]);
            resultSpan[i] = (uint)(n & 0xffffffff);
            carry = n >> UINT_BIT_SIZE;
         }

         return result;
      }

      public static Target operator /(Target dividend, Target divisor)
      {
         int dividendBits = dividend.Bits();
         int divisorBits = divisor.Bits();

         if (divisorBits == 0)
         {
            ThrowHelper.ThrowArgumentException("Division by zero");
         }

         // the result is certainly 0.
         if (divisorBits > dividendBits)
         {
            return Zero;
         }

         // the quotient.
         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         //result.Fill(0); //allocated data is already zeroed

         int shiftAmount = dividendBits - divisorBits;
         divisor <<= shiftAmount; // shift so that div and num align.
         while (shiftAmount >= 0)
         {
            if (dividend >= divisor)
            {
               dividend -= divisor;
               // set a bit of the result.
               result[shiftAmount / UINT_BIT_SIZE] |= (uint)(1 << (shiftAmount & (UINT_BIT_SIZE - 1)));
            }

            divisor >>= 1; // shift back.
            shiftAmount--;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator *(Target left, ulong right)
      {
         return left * FromRawValue(right);
      }

      public static Target operator /(Target left, ulong right)
      {
         return left / FromRawValue(right);
      }





      public void Multiply(uint value)
      {
         Span<uint> data = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, uint>(ref this.part1), UINT_ELEMENTS_COUNT);

         ulong carry = 0;
         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            ulong n = carry + ((ulong)value * data[i]);
            data[i] = (uint)(n & 0xffffffff);
            carry = n >> UINT_BIT_SIZE;
         }
      }

      public void Divide(uint value)
      {
         BigInteger dividend = new BigInteger(this.GetBytes());

         Span<byte> data = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref this.part1), EXPECTED_SIZE);
         data.Fill(0);
         (dividend / value).TryWriteBytes(data, out int writeBytes);



         //Span<uint> divisorData = stackalloc uint[UINT_ELEMENTS_COUNT];
         //divisorData[0] = divisor;

         //Span<uint> data = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, uint>(ref this.part1), UINT_ELEMENTS_COUNT);

         //ReadOnlySpan<uint> divisorBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref div.part1), UINT_ELEMENTS_COUNT);

         //int dividendBits = this.Bits();
         //int divisorBits = div.Bits();

         //if (divisorBits == 0)
         //{
         //   ThrowHelper.ThrowArgumentException("Division by zero");
         //}

         //// the result is certainly 0.
         //if (divisorBits > dividendBits)
         //{
         //   data.Fill(0);
         //   return;
         //}

         //// the quotient.
         //Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];

         //int shiftAmount = dividendBits - divisorBits;
         //divisor <<= shiftAmount; // shift so that div and num align.
         //while (shiftAmount >= 0)
         //{
         //   if (dividend >= divisor)
         //   {
         //      dividend -= divisor;
         //      // set a bit of the result.
         //      result[shiftAmount / UINT_BIT_SIZE] |= (uint)(1 << (shiftAmount & (UINT_BIT_SIZE - 1)));
         //   }

         //   divisor >>= 1; // shift back.
         //   shiftAmount--;
         //}

         //return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }
   }
}
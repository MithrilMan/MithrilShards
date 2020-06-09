using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.DataTypes
{
   public partial class Target : UInt256
   {
      const int UINT_ELEMENTS_COUNT = EXPECTED_SIZE / sizeof(uint);

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
            carry = n >> sizeof(uint);
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

         Span<uint> leftCopy = stackalloc uint[UINT_ELEMENTS_COUNT];
         leftBytes.CopyTo(leftCopy);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         result.Fill(0);

         int k = shiftAmount / sizeof(uint);
         shiftAmount %= sizeof(uint);

         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            if (i + k + 1 < UINT_ELEMENTS_COUNT && shiftAmount != 0)
               result[i + k + 1] |= leftCopy[i] >> (32 - shiftAmount);

            if (i + k < UINT_ELEMENTS_COUNT)
               result[i + k] |= leftCopy[i] << shiftAmount;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator >>(Target left, int shiftAmount)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);

         Span<uint> leftCopy = stackalloc uint[UINT_ELEMENTS_COUNT];
         leftBytes.CopyTo(leftCopy);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         result.Fill(0);

         int k = shiftAmount / sizeof(uint);
         shiftAmount %= sizeof(uint);

         for (int i = 0; i < UINT_ELEMENTS_COUNT; i++)
         {
            if (i + k + 1 < UINT_ELEMENTS_COUNT && shiftAmount != 0)
               result[i + k + 1] |= leftCopy[i] << (32 - shiftAmount);

            if (i + k < UINT_ELEMENTS_COUNT)
               result[i + k] |= leftCopy[i] >> shiftAmount;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }


      //public static Target operator +(Target? a, Target? b)
      //{
      //   // todo
      //   return a + b;
      //}

      internal uint ToCompact()
      {
         throw new NotImplementedException();
      }

      public static Target operator *(Target left, Target right)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref left.part1), UINT_ELEMENTS_COUNT);
         ReadOnlySpan<uint> rightBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref right.part1), UINT_ELEMENTS_COUNT);

         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];

         for (int j = 0; j < UINT_ELEMENTS_COUNT; j++)
         {
            ulong carry = 0;
            for (int i = 0; i + j < UINT_ELEMENTS_COUNT; i++)
            {
               ulong n = carry + result[i + j] + (ulong)(leftBytes[j] * rightBytes[i]);
               result[i + j] = (uint)(n & 0xffffffff);
               carry = n >> 32;
            }
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator /(Target numerator, Target denominator)
      {
         ReadOnlySpan<uint> leftBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref numerator.part1), UINT_ELEMENTS_COUNT);
         ReadOnlySpan<uint> rightBytes = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, uint>(ref denominator.part1), UINT_ELEMENTS_COUNT);

         int numeratorBits = numerator.Bits();
         int denominatorBits = denominator.Bits();

         if (denominatorBits == 0)
         {
            ThrowHelper.ThrowArgumentException("Division by zero");
         }

         // the result is certainly 0.
         if (denominatorBits > numeratorBits)
         {
            return Zero;
         }

         // the quotient.
         Span<uint> result = stackalloc uint[UINT_ELEMENTS_COUNT];
         result.Fill(0);

         int shiftAmount = numeratorBits - denominatorBits;
         numerator <<= shiftAmount; // shift so that div and num align.
         while (shiftAmount >= 0)
         {
            if (numerator >= denominator)
            {
               numerator -= denominator;
               result[shiftAmount / sizeof(uint)] |= (uint)(1 << (shiftAmount & (sizeof(uint) - 1))); // set a bit of the result.
            }

            denominator >>= 1; // shift back.
            shiftAmount--;
         }

         return new Target(MemoryMarshal.Cast<uint, byte>(result));
      }

      public static Target operator *(Target left, long right)
      {
         return left * FromValue((ulong)right);
      }

      public static Target operator *(Target left, ulong right)
      {
         return left * FromValue(right);
      }

      public static Target operator /(Target left, long right)
      {
         return left / FromValue((ulong)right);
      }

      public static Target operator /(Target left, ulong right)
      {
         return left / FromValue(right);
      }

      public static Target Add(Target left, Target right)
      {
         return left + right;
      }

      public static Target Subtract(Target left, Target right)
      {
         return left - right;
      }


      public static Target Multiply(Target left, Target right)
      {
         return left * right;
      }

      public static Target Divide(Target left, Target right)
      {
         return left / right;
      }

      public static Target LeftShift(Target left, int shiftAmount)
      {
         return left << shiftAmount;
      }

      public static Target RightShift(Target left, int shiftAmount)
      {
         return left >> shiftAmount;
      }

      public static Target Negate(Target item)
      {
         return -item;
      }
   }
}
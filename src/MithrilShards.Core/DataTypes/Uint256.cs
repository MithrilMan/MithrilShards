using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MithrilShards.Core.DataTypes
{
   [StructLayout(LayoutKind.Sequential)]
   public partial class UInt256 : IEquatable<UInt256>
   {
      protected const int EXPECTED_SIZE = 32;

      public static UInt256 Zero { get; } = new UInt256("0".PadRight(EXPECTED_SIZE * 2, '0'));

      protected ulong part1;
      protected ulong part2;
      protected ulong part3;
      protected ulong part4;

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256"/> class.
      /// Used by derived classes.
      /// </summary>
      protected UInt256() { }

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256"/>, expect data in Little Endian.
      /// </summary>
      /// <param name="data">The data.</param>
      public UInt256(ReadOnlySpan<byte> input)
      {
         if (input.Length != EXPECTED_SIZE)
         {
            ThrowHelper.ThrowFormatException("the byte array should be 32 bytes long");
         }

         Span<byte> dst = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref part1), EXPECTED_SIZE);
         input.CopyTo(dst);
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256"/> class.
      /// Passed hex string must be a valid hex string with 64 char length, or 66 if prefix 0x is used, otherwise an exception is thrown.
      /// Input data is considered in big endian.
      /// </summary>
      public UInt256(string hexString)
      {
         if (hexString is null)
         {
            ThrowHelper.ThrowArgumentNullException(nameof(hexString));
         }

         //account for 0x prefix
         if (hexString.Length < EXPECTED_SIZE * 2)
         {
            ThrowHelper.ThrowFormatException($"the hex string should be {EXPECTED_SIZE * 2} chars long or {(EXPECTED_SIZE * 2) + 4} if prefixed with 0x.");
         }

         ReadOnlySpan<char> hexAsSpan = (hexString[0] == '0' && (hexString[1] == 'X' || hexString[1] == 'x')) ? hexString.AsSpan(2) : hexString.AsSpan();

         if (hexAsSpan.Length != EXPECTED_SIZE * 2)
         {
            ThrowHelper.ThrowFormatException($"the hex string should be {EXPECTED_SIZE * 2} chars long or {(EXPECTED_SIZE * 2) + 4} if prefixed with 0x.");
         }

         Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref part1, EXPECTED_SIZE / sizeof(ulong)));

         int i = hexAsSpan.Length - 1;
         int j = 0;

         while (i > 0)
         {
            char c = hexAsSpan[i--];
            if (c >= '0' && c <= '9')
            {
               dst[j] = (byte)(c - '0');
            }
            else if (c >= 'a' && c <= 'f')
            {
               dst[j] = (byte)(c - ('a' - 10));
            }
            else if (c >= 'A' && c <= 'F')
            {
               dst[j] = (byte)(c - ('A' - 10));
            }
            else
            {
               ThrowHelper.ThrowArgumentException("Invalid nibble: " + c);
            }

            c = hexAsSpan[i--];
            if (c >= '0' && c <= '9')
            {
               dst[j] |= (byte)((c - '0') << 4);
            }
            else if (c >= 'a' && c <= 'f')
            {
               dst[j] |= (byte)((c - ('a' - 10)) << 4);
            }
            else if (c >= 'A' && c <= 'F')
            {
               dst[j] |= (byte)((c - ('A' - 10)) << 4);
            }
            else
            {
               ThrowHelper.ThrowArgumentException("Invalid nibble: " + c);
            }

            j++;
         }
      }


      /// <summary>
      /// Converts to string in hexadecimal format.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String" /> that represents this instance.
      /// </returns>
      public override string ToString()
      {
         return string.Create(EXPECTED_SIZE * 2, this, (dst, src) =>
         {
            ReadOnlySpan<byte> rawData = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref src.part1, EXPECTED_SIZE / sizeof(ulong)));

            const string HexValues = "0123456789ABCDEF";

            int i = rawData.Length - 1;
            int j = 0;

            while (i >= 0)
            {
               byte b = rawData[i--];
               dst[j++] = HexValues[b >> 4];
               dst[j++] = HexValues[b & 0xF];
            }
         });
      }


      /// <summary>
      /// Try to parse from an hex string to UInt256.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String" /> that represents this instance.
      /// </returns>
      public static bool TryParse(string hexString, out UInt256? result)
      {
         try
         {
            result = new UInt256(hexString);
            return true;
         }
         catch (Exception)
         {
            result = null;
         }

         return false;
      }

      /// <summary>
      /// Try to parse from an hex string to UInt256.
      /// </summary>
      /// <param name="hexString">The hexadecimal to parse.</param>
      /// <returns>
      /// A <see cref="System.String" /> that represents this instance.
      /// </returns>
      public static UInt256 Parse(string hexString)
      {
         return new UInt256(hexString);
      }

      public ReadOnlySpan<byte> GetBytes()
      {
         return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, byte>(ref part1), EXPECTED_SIZE);
      }

      public override int GetHashCode()
      {
         return (int)part1;
      }
   }
}

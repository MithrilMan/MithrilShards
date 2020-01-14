using System;
using System.Runtime.InteropServices;

namespace MithrilShards.Core.DataTypes
{
   [StructLayout(LayoutKind.Sequential)]
   public class UInt256
   {
      private const int EXPECTED_SIZE = 32;

      public static UInt256 Zero = new UInt256("0".PadRight(EXPECTED_SIZE * 2, '0'));

#pragma warning disable IDE0044 // Add readonly modifier
      private ulong part1;
      private ulong part2;
      private ulong part3;
      private ulong part4;
#pragma warning restore IDE0044 // Add readonly modifier

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256"/>, expect data in Little Endian.
      /// </summary>
      /// <param name="data">The data.</param>
      public UInt256(ReadOnlySpan<byte> input)
      {
         if (input.Length != EXPECTED_SIZE)
         {
            throw new FormatException("the byte array should be 32 bytes long");
         }

         Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));
         input.CopyTo(dst);
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256"/> class.
      /// Passed hex string must be a valid hex string with 64 char length, or 66 if prefix 0x is used, otherwise an exception is thrown.
      /// </summary>
      public UInt256(string hexString)
      {
         if (hexString is null)
         {
            throw new ArgumentNullException(nameof(hexString));
         }

         //account for 0x prefix
         if (hexString.Length < EXPECTED_SIZE * 2)
         {
            throw new FormatException($"the hex string should be {EXPECTED_SIZE * 2} chars long or {(EXPECTED_SIZE * 2) + 4} if prefixed with 0x.");
         }

         ReadOnlySpan<char> hexAsSpan = (hexString[0] == '0' && hexString[1] == 'X') ? hexString.AsSpan(2) : hexString.AsSpan();

         if (hexString.Length != EXPECTED_SIZE * 2)
         {
            throw new FormatException($"the hex string should be {EXPECTED_SIZE * 2} chars long or {(EXPECTED_SIZE * 2) + 4} if prefixed with 0x.");
         }

         Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));

         int i = hexString.Length - 1;
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
               throw new ArgumentException("Invalid nibble: " + c);
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
               throw new ArgumentException("Invalid nibble: " + c);
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
      public static bool TryParse(string hexString, out UInt256 result)
      {
         try
         {
            result = new UInt256(hexString);
            return true;
         }
         catch
         {
            result = null;
            return false;
         }
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
         return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));
      }

      public override int GetHashCode()
      {
         return (int)this.part1;
      }

      public override bool Equals(object obj)
      {
         return ReferenceEquals(this, obj) ? true : this.Equals(obj);
      }

      public bool Equals(UInt256 other)
      {
         if (other is null) return false;

         return this.part1 == other.part1
             && this.part2 == other.part2
             && this.part3 == other.part3
             && this.part4 == other.part4;
      }
   }
}

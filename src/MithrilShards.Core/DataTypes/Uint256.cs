using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MithrilShards.Core.Encoding;

namespace MithrilShards.Core.DataTypes {
   [StructLayout(LayoutKind.Sequential)]
   public class UInt256 {
      private const int EXPECTED_SIZE = 32;
      private static readonly byte[] HexValues = new byte[]       {
         0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
      };

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
      public UInt256(ReadOnlySpan<byte> input) {
         Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));
         input[..EXPECTED_SIZE].CopyTo(dst);
      }

      /// <summary>
      /// Converts to string in hexadecimal format.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String" /> that represents this instance.
      /// </returns>
      public override string ToString() {
         return string.Create(EXPECTED_SIZE * 3 - 1, this, (dst, src) => {
            ReadOnlySpan<byte> rawData = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref src.part1, EXPECTED_SIZE / sizeof(ulong)));

            const string HexValues = "0123456789ABCDEF";

            int i = rawData.Length - 1;
            int j = 0;

            while (i >= 0) {
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
      public static bool TryParse(string hex, out UInt256 result) {
         if (hex is null) {
            throw new ArgumentNullException(nameof(hex));
         }

         //account for 0x prefix
         if (hex.Length <= EXPECTED_SIZE * 2) {
            result = null;
            return false;
         }

         ReadOnlySpan<char> hexAsSpan = (hex[0] == '0' && hex[1] == 'X') ? hex.AsSpan(2) : hex.AsSpan();

         if (hex.Length != EXPECTED_SIZE * 2) {
            result = null;
            return false;
         }

         Span<byte> bytes = stackalloc byte[EXPECTED_SIZE];

         int i = bytes.Length - 1;
         int j = 0;

         while (i >= 0) {
            //bytes[i--] = HexEncoder.HexStringTable[hexAsSpan[j++] << 4 | hexAsSpan[j++]];
            bytes[i--] = (byte)(ParseNibble(hexAsSpan[j++]) << 4 | ParseNibble(hexAsSpan[j++]));
         }

         result = new UInt256(bytes);
         return true;
      }

      private static byte ParseNibble(char c) {
         if (c >= '0' && c <= '9') {
            return (byte)(c - '0');
         }
         c = (char)(c & ~0x20);
         if (c >= 'A' && c <= 'F') {
            return (byte)(c - ('A' - 10));
         }
         throw new ArgumentException("Invalid nibble: " + c);
      }
   }
}

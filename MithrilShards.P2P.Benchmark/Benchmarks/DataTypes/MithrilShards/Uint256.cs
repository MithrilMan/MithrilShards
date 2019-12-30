using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.MithrilShards {
   public readonly struct UInt256 {
      private const int EXPECTED_SIZE = 32;
      private static readonly NBitcoin.DataEncoders.HexEncoder Encoder = new NBitcoin.DataEncoders.HexEncoder();

      private readonly byte[] bytes;

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256"/>, expect data in Little Endian.
      /// </summary>
      /// <param name="input">The data.</param>
      public UInt256(ReadOnlySpan<byte> input) {
         //if (input.Length != EXPECTED_SIZE) {
         //   throw new FormatException("the byte array should be 32 bytes long");
         //}

         this.bytes = input.ToArray();
      }

      public override string ToString() {
         Span<byte> span = stackalloc byte[32];
         this.bytes.CopyTo(span);
         span.Reverse();
         return Encoder.EncodeData(span.ToArray());
      }
   }

   public readonly struct UInt256As4Long {
      private const int EXPECTED_SIZE = 32;
      private static readonly NBitcoin.DataEncoders.HexEncoder Encoder = new NBitcoin.DataEncoders.HexEncoder();

      private readonly long[] data;

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256"/>, expect data in Little Endian.
      /// </summary>
      /// <param name="data">The data.</param>
      public UInt256As4Long(ReadOnlySpan<byte> input) {
         //if (input.Length != EXPECTED_SIZE) {
         //   throw new FormatException("the byte array should be 32 bytes long");
         //}

         this.data = MemoryMarshal.Cast<byte, long>(input).ToArray();
      }

      public override string ToString() {
         Span<byte> toBeReversed = MemoryMarshal.Cast<long,byte>(this.data).ToArray();
         toBeReversed.Reverse();
         return Encoder.EncodeData(toBeReversed.ToArray());
      }
   }
}
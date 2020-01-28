using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class Uint256_Study
   {
      private readonly byte[] data = new byte[32];
      Core.DataTypes.UInt256 u1,u2;
      MM_UInt256 uM1, uM2;

      [GlobalSetup]
      public void Setup()
      {
         new Random(47).NextBytes(this.data);

         u1 = new Core.DataTypes.UInt256(this.data);
         u2 = new Core.DataTypes.UInt256(this.data);

         new Random(53).NextBytes(this.data);
         uM1 = new MM_UInt256(this.data);
         uM2 = new MM_UInt256(this.data);
      }

      [Benchmark]
      public object uint256_MithrilShards_CurrentImplementation()
      {
         return new MithrilShards.Core.DataTypes.UInt256(this.data);
      }

      [Benchmark]
      public object uint256_MithrilShards_FromArray()
      {
         return new MM_UInt256(this.data);
      }

      [Benchmark]
      public bool uint256_MithrilShards_CurrentImplementation_Equality()
      {
         return u1 == u2;
      }

      [Benchmark]
      public bool uint256_MithrilShards_FromArray_Equality()
      {
         return uM1 == uM2;
      }
   }



   public class MM_UInt256 : IEquatable<MM_UInt256>
   {
      private const int EXPECTED_SIZE = 32;

      public static MM_UInt256 Zero { get; } = new MM_UInt256("0".PadRight(EXPECTED_SIZE * 2, '0'));

#pragma warning disable IDE0044 // Add readonly modifier
      protected ulong[] parts = new ulong[4];

#pragma warning restore IDE0044 // Add readonly modifier

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256"/>, expect data in Little Endian.
      /// </summary>
      /// <param name="data">The data.</param>
      public MM_UInt256(ReadOnlySpan<byte> input)
      {
         if (input.Length != EXPECTED_SIZE)
         {
            throw new FormatException("the byte array should be 32 bytes long");
         }

         Span<byte> dst = MemoryMarshal.AsBytes<ulong>(this.parts);
         input.CopyTo(dst);
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256"/> class.
      /// Passed hex string must be a valid hex string with 64 char length, or 66 if prefix 0x is used, otherwise an exception is thrown.
      /// </summary>
      public MM_UInt256(string hexString)
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

         Span<byte> dst = MemoryMarshal.AsBytes<ulong>(this.parts);

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
            ReadOnlySpan<byte> rawData = MemoryMarshal.AsBytes<ulong>(src.parts);

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
      public static bool TryParse(string hexString, out MM_UInt256? result)
      {
         try
         {
            result = new MM_UInt256(hexString);
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
      public static MM_UInt256 Parse(string hexString)
      {
         return new MM_UInt256(hexString);
      }

      public ReadOnlySpan<byte> GetBytes()
      {
         return MemoryMarshal.AsBytes<ulong>(this.parts);
      }

      public override int GetHashCode()
      {
         return (int)this.parts[0];
      }

      public override bool Equals(object? obj) => ReferenceEquals(this, obj) ? true : this.Equals(obj as MM_UInt256);

      public static bool operator !=(MM_UInt256? a, MM_UInt256? b) => !(a == b);

      public static bool operator ==(MM_UInt256? a, MM_UInt256? b) => a is null ? false : a.Equals(b);

      public bool Equals(MM_UInt256? other)
      {
         if (other is null) return false;

         return this.parts.AsSpan().SequenceEqual(other.parts.AsSpan());
      }
   }
}

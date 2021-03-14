using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser, PlainExporter]
   public class Uint256_Study
   {
      private readonly byte[] _data = new byte[32];
      AltUInt256.UInt256 _u1, _u2;
      AltUInt256.UInt256_Updated _aU1, _aU2;

      [GlobalSetup]
      public void Setup()
      {
         new Random(47).NextBytes(_data);

         _u1 = new AltUInt256.UInt256(_data);
         _aU1 = new AltUInt256.UInt256_Updated(_data);

         new Random(53).NextBytes(_data);
         _u2 = new AltUInt256.UInt256(_data);
         _aU2 = new AltUInt256.UInt256_Updated(_data);
      }

      [Benchmark]
      public object Create_UInt256()
      {
         return new AltUInt256.UInt256(_data);
      }

      [Benchmark]
      public object Create_AltUInt256()
      {
         return new AltUInt256.UInt256(_data);
      }

      [Benchmark]
      public ReadOnlySpan<byte> GetBytes_UInt256()
      {
         return _u1.GetBytes();
      }

      [Benchmark]
      public ReadOnlySpan<byte> GetBytes_AltUInt256()
      {
         return _aU1.GetBytes();
      }
   }
}


namespace AltUInt256
{
   [StructLayout(LayoutKind.Sequential)]
   public class UInt256
   {
      protected const int EXPECTED_SIZE = 32;

      protected ulong part1;
      protected ulong part2;
      protected ulong part3;
      protected ulong part4;

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256" />, expect data in Little Endian.
      /// </summary>
      /// <param name="input">The byte array representing the UInt256 in little endian.</param>
      public UInt256(ReadOnlySpan<byte> input)
      {
         if (input.Length != EXPECTED_SIZE)
         {
            ThrowHelper.ThrowFormatException("the byte array should be 32 bytes long");
         }

         Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref part1, EXPECTED_SIZE / sizeof(ulong)));
         input.CopyTo(dst);
      }

      public ReadOnlySpan<byte> GetBytes()
      {
         return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref part1, EXPECTED_SIZE / sizeof(ulong)));
      }
   }

   [StructLayout(LayoutKind.Sequential)]
   public class UInt256_Updated
   {
      protected const int EXPECTED_SIZE = 32;

      protected ulong part1;
      protected ulong part2;
      protected ulong part3;
      protected ulong part4;

      /// <summary>
      /// Initializes a new instance of the <see cref="UInt256" />, expect data in Little Endian.
      /// </summary>
      /// <param name="input">The input.</param>
      public UInt256_Updated(ReadOnlySpan<byte> input)
      {
         if (input.Length != EXPECTED_SIZE)
         {
            ThrowHelper.ThrowFormatException("the byte array should be 32 bytes long");
         }

         Span<byte> dst = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref part1), EXPECTED_SIZE);
         input.CopyTo(dst);
      }

      public ReadOnlySpan<byte> GetBytes()
      {
         return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, byte>(ref part1), EXPECTED_SIZE);
      }
   }


   //   [StructLayout(LayoutKind.Sequential)]
   //   public struct UInt256_Struct
   //   {
   //      private const int EXPECTED_SIZE = 32;

   //#pragma warning disable IDE0044 // Add readonly modifier
   //      private ulong part1;
   //      private ulong part2;
   //      private ulong part3;
   //      private ulong part4;
   //#pragma warning restore IDE0044 // Add readonly modifier

   //      /// <summary>
   //      /// Initializes a new instance of the <see cref="UInt256"/>, expect data in Little Endian.
   //      /// </summary>
   //      /// <param name="data">The data.</param>
   //      public unsafe UInt256_Struct(ReadOnlySpan<byte> input)
   //      {
   //         if (input.Length != EXPECTED_SIZE)
   //         {
   //            getHeader
   //            ThrowHelper.ThrowFormatException("the byte array should be 32 bytes long");
   //         }

   //         fixed (byte* bytePointer = input)
   //         {

   //         }


   //         Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));
   //         input.CopyTo(dst);
   //      }

   //      public ReadOnlySpan<byte> GetBytes()
   //      {
   //         return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, byte>(ref this.part1), 32);
   //         //return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));
   //      }
   //   }


   public static class ThrowHelper
   {
      [DoesNotReturn]
      public static void ThrowFormatException(string message)
      {
         throw new FormatException("the byte array should be 32 bytes long");
      }
   }
}

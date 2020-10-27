using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class CompareUInt256
   {
      [Params(47, 83, 131)]
      public int N;

      private readonly byte[] data1 = new byte[32];
      Core.DataTypes.UInt256 reference1;

      private readonly byte[] data2 = new byte[32];
      Core.DataTypes.UInt256 reference2;

      [GlobalSetup]
      public void Setup()
      {
         var random = new Random(this.N);
         random.NextBytes(this.data1);
         this.reference1 = new Core.DataTypes.UInt256(this.data1);

         random.NextBytes(this.data2);
         this.reference2 = new Core.DataTypes.UInt256(this.data2);
      }

      [Benchmark]
      public bool As256()
      {
         return  DoubleSha512AsUInt256(this.data1) < (this.reference2);
      }

      [Benchmark]
      public bool As256FromBytes()
      {
         return new Core.DataTypes.UInt256(DoubleSha512AsBytes(this.data1)) < (this.reference2);
      }

      [Benchmark]
      public bool AsBytes()
      {
         return DoubleSha512AsBytes(this.data1).SequenceCompareTo(data2) == -1;
      }


      public static ReadOnlySpan<byte> DoubleSha512AsBytes(ReadOnlySpan<byte> data)
      {
         using (var sha = new SHA512Managed())
         {
            Span<byte> result = new byte[64];
            sha.TryComputeHash(data, result, out _);
            sha.TryComputeHash(result, result, out _);
            return result.Slice(0, 32);
         }
      }

      public static Core.DataTypes.UInt256 DoubleSha512AsUInt256(ReadOnlySpan<byte> data)
      {
         using (var sha = new SHA512Managed())
         {
            Span<byte> result = stackalloc byte[64];
            sha.TryComputeHash(data, result, out _);
            sha.TryComputeHash(result, result, out _);
            return new Core.DataTypes.UInt256(result.Slice(0, 32));
         }
      }
   }
}

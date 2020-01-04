using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class Uint256_FromBytes_Fight_Finalists
   {
      private readonly byte[] data;

      [GlobalSetup]
      public void Setup()
      {
         new Random().NextBytes(this.data);
      }

      [Benchmark]
      public void UInt256_Neo_FromBytes()
      {
         _ = new P2P.Benchmark.Benchmarks.DataTypes.Neo.UInt256(this.data);
      }

      [Benchmark]
      public void uint256_NBitcoin_FromBytes()
      {
         _ = new uint256(new ReadOnlySpan<byte>(this.data));
      }

      [Benchmark]
      public void uint256_Unsafe_MithrilShards4Longs_FromBytes()
      {
         _ = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UnsafeUInt256As4Long(this.data);
      }

      [Benchmark]
      public void uint256_Unsafe_MithrilShardsUInt256As4Jhon_FromBytes()
      {
         _ = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UInt256As4Jhon(this.data);
      }
   }
}

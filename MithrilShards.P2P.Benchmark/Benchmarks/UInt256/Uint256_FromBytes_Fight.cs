using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class Uint256_FromBytes_Fight
   {
      private readonly byte[] _data = new byte[32];

      [GlobalSetup]
      public void Setup()
      {
         new Random().NextBytes(_data);
      }

      [Benchmark]
      public void UInt256_BigInteger_FromBytes()
      {
         _ = new BigInteger(_data);
      }

      [Benchmark]
      public void UInt256_Neo_FromBytes()
      {
         _ = new P2P.Benchmark.Benchmarks.DataTypes.Neo.NEO_UInt256(_data);
      }

      [Benchmark]
      public void uint256_NBitcoin_FromBytes()
      {
         _ = new uint256(new ReadOnlySpan<byte>(_data));
      }

      [Benchmark]
      public void uint256_Stratis_FromBytes()
      {
         _ = new P2P.Benchmark.Benchmarks.DataTypes.Stratis.STRATIS_uint256(_data);
      }

      [Benchmark]
      public void uint256_MithrilShards_FromBytes()
      {
         _ = new Core.DataTypes.UInt256(_data);
      }

      [Benchmark]
      public void uint256_MithrilShards4Longs_FromBytes()
      {
         _ = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UInt256As4Long(_data);
      }

      [Benchmark]
      public void uint256_Unsafe_MithrilShards_FromBytes()
      {
         _ = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UnsafeUInt256(_data);
      }

      [Benchmark]
      public void uint256_Unsafe_MithrilShards4Longs_FromBytes()
      {
         _ = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UnsafeUInt256As4Long(_data);
      }

      [Benchmark]
      public void uint256_Unsafe_MithrilShardsUInt256As4Jhon_FromBytes()
      {
         _ = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UInt256As4Jhon(_data);
      }
   }
}

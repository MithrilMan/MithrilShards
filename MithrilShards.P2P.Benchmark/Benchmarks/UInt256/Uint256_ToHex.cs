using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class Uint256_ToHex
   {
      private NBitcoin.uint256 NBitcoinData;
      private MithrilShards.Core.DataTypes.UInt256 MithrilShardsData;

      [GlobalSetup]
      public void Setup()
      {
         var value = new Span<byte>(new byte[32]);
         new Random().NextBytes(value);

         this.NBitcoinData = new uint256(value.ToArray());
         this.MithrilShardsData = new Core.DataTypes.UInt256(value);
      }

      [Benchmark(Baseline = true)]
      public void UInt256_ToString_NBitcoin()
      {
         _ = this.NBitcoinData.ToString();
      }

      [Benchmark]
      public void UInt256_ToString_MithrilShards()
      {
         _ = this.MithrilShardsData.ToString();
      }
   }
}

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class Uint256_Equals
   {
      private NBitcoin.uint256 NBitcoinData, NBitcoinData2;
      private MithrilShards.Core.DataTypes.UInt256 MithrilShardsData, MithrilShardsData2;
      private MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Neo.NEO_UInt256 NeoData, NeoData2;

      [GlobalSetup]
      public void Setup()
      {
         Span<byte> value = new Span<byte>(new byte[32]);
         new Random().NextBytes(value);

         this.NBitcoinData = new uint256(value.ToArray());
         this.MithrilShardsData = new Core.DataTypes.UInt256(value);
         this.NeoData = new P2P.Benchmark.Benchmarks.DataTypes.Neo.NEO_UInt256(value);

         new Random().NextBytes(value);

         this.NBitcoinData2 = new uint256(value.ToArray());
         this.MithrilShardsData2 = new Core.DataTypes.UInt256(value);
         this.NeoData2 = new P2P.Benchmark.Benchmarks.DataTypes.Neo.NEO_UInt256(value);
      }

      [Benchmark(Baseline = true)]
      public bool NBitcoin()
      {
         return this.NBitcoinData.Equals(this.NBitcoinData2);
      }

      [Benchmark]
      public bool Neo()
      {
         return this.NeoData.Equals(this.NeoData2);
      }

      [Benchmark]
      public bool MithrilShards()
      {
         return this.MithrilShardsData.Equals(this.MithrilShardsData2);
      }
   }
}

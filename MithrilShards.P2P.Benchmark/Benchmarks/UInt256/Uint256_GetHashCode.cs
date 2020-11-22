using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class Uint256_GetHashCode
   {
      private NBitcoin.uint256 NBitcoinData;
      private MithrilShards.Core.DataTypes.UInt256 MithrilShardsData;
      private long long1;
      private long long2;
      private long long3;
      private long long4;

      [GlobalSetup]
      public void Setup()
      {
         var value = new Span<byte>(new byte[32]);
         new Random().NextBytes(value);

         this.NBitcoinData = new uint256(value.ToArray());
         this.MithrilShardsData = new Core.DataTypes.UInt256(value);

         this.long1 = (long)new Random().NextDouble();
         this.long2 = (long)new Random().NextDouble();
         this.long3 = (long)new Random().NextDouble();
         this.long4 = (long)new Random().NextDouble();
      }

      [Benchmark(Baseline = true)]
      public int NBitcoin()
      {
         return this.NBitcoinData.GetHashCode();
      }

      [Benchmark]
      public int MithrilShards()
      {
         return this.MithrilShardsData.GetHashCode();
      }

      [Benchmark]
      public int Direct()
      {
         return (int)this.long1;
      }

      [Benchmark]
      public int Combine()
      {
         return (int)HashCode.Combine(this.long1, this.long2, this.long3, this.long4);
      }
   }
}

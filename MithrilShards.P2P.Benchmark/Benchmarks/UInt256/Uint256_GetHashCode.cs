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
      private NBitcoin.uint256 _nBitcoinData;
      private MithrilShards.Core.DataTypes.UInt256 _mithrilShardsData;
      private long _long1;
      private long _long2;
      private long _long3;
      private long _long4;

      [GlobalSetup]
      public void Setup()
      {
         var value = new Span<byte>(new byte[32]);
         new Random().NextBytes(value);

         this._nBitcoinData = new uint256(value.ToArray());
         this._mithrilShardsData = new Core.DataTypes.UInt256(value);

         this._long1 = (long)new Random().NextDouble();
         this._long2 = (long)new Random().NextDouble();
         this._long3 = (long)new Random().NextDouble();
         this._long4 = (long)new Random().NextDouble();
      }

      [Benchmark(Baseline = true)]
      public int NBitcoin()
      {
         return this._nBitcoinData.GetHashCode();
      }

      [Benchmark]
      public int MithrilShards()
      {
         return this._mithrilShardsData.GetHashCode();
      }

      [Benchmark]
      public int Direct()
      {
         return (int)this._long1;
      }

      [Benchmark]
      public int Combine()
      {
         return (int)HashCode.Combine(this._long1, this._long2, this._long3, this._long4);
      }
   }
}

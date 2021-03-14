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

         _nBitcoinData = new uint256(value.ToArray());
         _mithrilShardsData = new Core.DataTypes.UInt256(value);

         _long1 = (long)new Random().NextDouble();
         _long2 = (long)new Random().NextDouble();
         _long3 = (long)new Random().NextDouble();
         _long4 = (long)new Random().NextDouble();
      }

      [Benchmark(Baseline = true)]
      public int NBitcoin()
      {
         return _nBitcoinData.GetHashCode();
      }

      [Benchmark]
      public int MithrilShards()
      {
         return _mithrilShardsData.GetHashCode();
      }

      [Benchmark]
      public int Direct()
      {
         return (int)_long1;
      }

      [Benchmark]
      public int Combine()
      {
         return (int)HashCode.Combine(_long1, _long2, _long3, _long4);
      }
   }
}

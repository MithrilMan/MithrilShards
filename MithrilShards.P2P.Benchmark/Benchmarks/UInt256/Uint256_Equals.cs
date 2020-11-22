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
      private NBitcoin.uint256 _nBitcoinData, _nBitcoinData2;
      private MithrilShards.Core.DataTypes.UInt256 _mithrilShardsData, _mithrilShardsData2;
      private MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Neo.NEO_UInt256 _neoData, _neoData2;

      [GlobalSetup]
      public void Setup()
      {
         var value = new Span<byte>(new byte[32]);
         new Random().NextBytes(value);

         this._nBitcoinData = new uint256(value.ToArray());
         this._mithrilShardsData = new Core.DataTypes.UInt256(value);
         this._neoData = new P2P.Benchmark.Benchmarks.DataTypes.Neo.NEO_UInt256(value);

         new Random().NextBytes(value);

         this._nBitcoinData2 = new uint256(value.ToArray());
         this._mithrilShardsData2 = new Core.DataTypes.UInt256(value);
         this._neoData2 = new P2P.Benchmark.Benchmarks.DataTypes.Neo.NEO_UInt256(value);
      }

      [Benchmark(Baseline = true)]
      public bool NBitcoin()
      {
         return this._nBitcoinData.Equals(this._nBitcoinData2);
      }

      [Benchmark]
      public bool Neo()
      {
         return this._neoData.Equals(this._neoData2);
      }

      [Benchmark]
      public bool MithrilShards()
      {
         return this._mithrilShardsData.Equals(this._mithrilShardsData2);
      }
   }
}

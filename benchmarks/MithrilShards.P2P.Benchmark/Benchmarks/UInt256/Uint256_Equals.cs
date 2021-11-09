using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256;

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

      _nBitcoinData = new uint256(value.ToArray());
      _mithrilShardsData = new Core.DataTypes.UInt256(value);
      _neoData = new P2P.Benchmark.Benchmarks.DataTypes.Neo.NEO_UInt256(value);

      new Random().NextBytes(value);

      _nBitcoinData2 = new uint256(value.ToArray());
      _mithrilShardsData2 = new Core.DataTypes.UInt256(value);
      _neoData2 = new P2P.Benchmark.Benchmarks.DataTypes.Neo.NEO_UInt256(value);
   }

   [Benchmark(Baseline = true)]
   public bool NBitcoin()
   {
      return _nBitcoinData.Equals(_nBitcoinData2);
   }

   [Benchmark]
   public bool Neo()
   {
      return _neoData.Equals(_neoData2);
   }

   [Benchmark]
   public bool MithrilShards()
   {
      return _mithrilShardsData.Equals(_mithrilShardsData2);
   }
}

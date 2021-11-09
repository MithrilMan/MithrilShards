using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256;

[SimpleJob(RuntimeMoniker.NetCoreApp31)]
[RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
public class Uint256_ToHex
{
   private NBitcoin.uint256 _nBitcoinData;
   private MithrilShards.Core.DataTypes.UInt256 _mithrilShardsData;

   [GlobalSetup]
   public void Setup()
   {
      var value = new Span<byte>(new byte[32]);
      new Random().NextBytes(value);

      _nBitcoinData = new uint256(value.ToArray());
      _mithrilShardsData = new Core.DataTypes.UInt256(value);
   }

   [Benchmark(Baseline = true)]
   public void UInt256_ToString_NBitcoin()
   {
      _ = _nBitcoinData.ToString();
   }

   [Benchmark]
   public void UInt256_ToString_MithrilShards()
   {
      _ = _mithrilShardsData.ToString();
   }
}

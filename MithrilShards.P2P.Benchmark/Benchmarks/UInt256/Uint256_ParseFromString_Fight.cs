using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Neo;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks.UInt256
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class Uint256_ParseFromString_Fight
   {
      private string _data;

      [GlobalSetup]
      public void Setup()
      {
         var value = new Span<byte>(new byte[32]);
         new Random().NextBytes(value);
         this._data = value.ToArray().ToHexString();
      }

      [Benchmark]
      public void UInt256_Neo_Parse()
      {
         _ = NEO_UInt256.Parse(this._data);
      }

      [Benchmark]
      public void uint256_NBitcoin_Parse()
      {
         _ = uint256.Parse(this._data);
      }

      [Benchmark]
      public void uint256_NBitcoin_StringConstructor()
      {
         _ = new uint256(this._data);
      }

      [Benchmark]
      public void uint256_MithrilShards_Parse()
      {
         _ = MithrilShards.Core.DataTypes.UInt256.Parse(this._data);
      }

      [Benchmark]
      public void uint256_MithrilShards_StringConstructor()
      {
         _ = new MithrilShards.Core.DataTypes.UInt256(this._data);
      }
   }
}

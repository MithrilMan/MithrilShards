using System;
using System.Numerics;
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
      private string data;

      [GlobalSetup]
      public void Setup()
      {
         Span<byte> value = new Span<byte>(new byte[32]);
         new Random().NextBytes(value);
         this.data = value.ToArray().ToHexString();
      }

      [Benchmark]
      public void UInt256_BigInteger_Parse()
      {
         _ = BigInteger.Parse(this.data, System.Globalization.NumberStyles.HexNumber);
      }

      [Benchmark]
      public void UInt256_Neo_Parse()
      {
         _ = P2P.Benchmark.Benchmarks.DataTypes.Neo.UInt256.Parse(this.data);
      }

      [Benchmark]
      public void uint256_NBitcoin_Parse()
      {
         _ = uint256.Parse(this.data);
      }

      [Benchmark]
      public void uint256_NBitcoin_StringConstructor()
      {
         _ = new uint256(this.data);
      }

      [Benchmark]
      public void uint256_MithrilShards_Parse()
      {
         _ = MithrilShards.Core.DataTypes.UInt256.Parse(this.data);
      }

      [Benchmark]
      public void uint256_MithrilShards_StringConstructor()
      {
         _ = new MithrilShards.Core.DataTypes.UInt256(this.data);
      }
   }
}

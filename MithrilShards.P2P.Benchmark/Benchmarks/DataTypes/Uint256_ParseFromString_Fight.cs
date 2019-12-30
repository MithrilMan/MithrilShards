using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;
using MithrilShards.Core.Crypto;
using MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Neo;

namespace MithrilShards.Network.Benchmark.Benchmarks.DataTypes {
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class Uint256_ParseFromString_Fight {
      private string[] data;


      [Params(1000)]
      public int N;

      [GlobalSetup]
      public void Setup() {
         this.data = new string[this.N];
         for (int i = 0; i < this.N; i++) {
            Span<byte> value = new Span<byte>(new byte[32]);
            new Random().NextBytes(value);
            this.data[i] = value.ToArray().ToHexString();
         }
      }

      [Benchmark]
      public void UInt256_BigInteger_Parse() {
         for (int i = 0; i < this.N; i++) {
            BigInteger.Parse(this.data[i], System.Globalization.NumberStyles.HexNumber);
         }
      }

      [Benchmark]
      public void UInt256_Neo_Parse() {
         for (int i = 0; i < this.N; i++) {
            P2P.Benchmark.Benchmarks.DataTypes.Neo.UInt256.Parse(this.data[i]);
         }
      }

      [Benchmark]
      public void uint256_NBitcoin_Parse() {
         for (int i = 0; i < this.N; i++) {
            uint256.Parse(this.data[i]);
         }
      }
   }
}

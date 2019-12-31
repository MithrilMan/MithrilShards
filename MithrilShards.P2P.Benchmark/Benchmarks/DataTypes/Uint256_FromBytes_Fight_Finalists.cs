using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;
using MithrilShards.Core.Crypto;
using System.Collections.Generic;

namespace MithrilShards.Network.Benchmark.Benchmarks.DataTypes {
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class Uint256_FromBytes_Fight_Finalists {
      private List<byte[]> data;


      [Params(1000)]
      public int N;

      [GlobalSetup]
      public void Setup() {
         this.data = new List<byte[]>();
         for (int i = 0; i < this.N; i++) {
            Span<byte> value = new Span<byte>(new byte[32]);
            new Random().NextBytes(value);
            this.data.Add(value.ToArray());
         }
      }

      [Benchmark]
      public void UInt256_Neo_FromBytes() {
         for (int i = 0; i < this.N; i++) {
            _ = new P2P.Benchmark.Benchmarks.DataTypes.Neo.UInt256(this.data[i]);
         }
      }

      [Benchmark]
      public void uint256_NBitcoin_FromBytes() {
         for (int i = 0; i < this.N; i++) {
            _ = new uint256(new ReadOnlySpan<byte>(this.data[i]));
         }
      }

      [Benchmark]
      public void uint256_Unsafe_MithrilShards4Longs_FromBytes() {
         for (int i = 0; i < this.N; i++) {
            _ = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UnsafeUInt256As4Long(this.data[i]);
         }
      }

      [Benchmark]
      public void uint256_Unsafe_MithrilShardsUInt256As4Jhon_FromBytes() {
         for (int i = 0; i < this.N; i++) {
            _ = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UInt256As4Jhon(this.data[i]);
         }
      }
   }
}

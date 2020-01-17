using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MithrilShards.Core.Crypto;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class SHA256
   {

      private byte[] data;

      [Params(1000)]
      public int Payload;

      [GlobalSetup]
      public void Setup()
      {
         this.data = new byte[this.Payload];
         new Random().NextBytes(this.data);
      }


      [Benchmark]
      public NBitcoin.uint256 NBitcoin_Hash256()
      {
         return NBitcoin.Crypto.Hashes.Hash256(this.data);
      }

      [Benchmark]
      public Core.DataTypes.UInt256 MithrilShards_DoubleSha256()
      {
         return new Core.DataTypes.UInt256(HashGenerator.DoubleSha256(this.data));
      }
   }
}

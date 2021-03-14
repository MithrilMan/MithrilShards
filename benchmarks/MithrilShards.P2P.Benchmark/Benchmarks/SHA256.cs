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

      private byte[] _data;

      [Params(1000)]
      public int Payload;

      [GlobalSetup]
      public void Setup()
      {
         _data = new byte[Payload];
         new Random().NextBytes(_data);
      }


      [Benchmark]
      public NBitcoin.uint256 NBitcoin_Hash256()
      {
         return NBitcoin.Crypto.Hashes.DoubleSHA256(_data);
      }

      [Benchmark]
      public Core.DataTypes.UInt256 MithrilShards_DoubleSha256()
      {
         return new Core.DataTypes.UInt256(HashGenerator.DoubleSha256(_data));
      }
   }
}

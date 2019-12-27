using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using MithrilShards.Core.Crypto;

namespace MithrilShards.Network.Benchmark.Benchmarks {
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RPlotExporter, RankColumn, MarkdownExporterAttribute.GitHub, CsvMeasurementsExporter]
   public class SHA256 {

      private byte[] data;

      [Params(100, 10000)]
      public int Payload;

      [Params(10, 100, 1000)]
      public int N;

      [GlobalSetup]
      public void Setup() {
         this.data = new byte[this.Payload];
         new Random().NextBytes(this.data);
      }


      [Benchmark]
      public void NBitcoin_Hash256() {
         for (int i = 0; i < this.N; i++) {
            NBitcoin.Crypto.Hashes.Hash256(this.data);
         }
      }

      [Benchmark]
      public void MithrilShards_DoubleSha256() {
         for (int i = 0; i < this.N; i++) {
            new NBitcoin.uint256(HashGenerator.DoubleSha256(this.data));
         }
      }
   }
}

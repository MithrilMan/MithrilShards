using System;
using System.Numerics;
using System.Security.Cryptography;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using MithrilShards.Core.Crypto;
using MithrilShards.Network.Benchmark.Benchmarks;

namespace MithrilShards.Network.Benchmark {
   class Program {

      public class Config : ManualConfig {
         public Config() {
            //this.Add(CsvMeasurementsExporter.Default);
            //this.Add(RPlotExporter.Default);
         }
      }

      static void Main(string[] args) {
         //BenchmarkRunner.Run<BigInt_vs_uint256_parse>();
         //BenchmarkRunner.Run<MagicNumberFinder>();
         BenchmarkRunner.Run<Benchmarks.SHA256>();
      }
   }
}

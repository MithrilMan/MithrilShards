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
         byte[] data = new byte[32];
         new Random().NextBytes(data);

         //string val = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UInt256(data).ToString();
         //string val1 = new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UInt256As4Long(data).ToString();
         //string val2 = new NBitcoin.uint256(new ReadOnlySpan<byte>(data)).ToString();

         BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
      }
   }
}

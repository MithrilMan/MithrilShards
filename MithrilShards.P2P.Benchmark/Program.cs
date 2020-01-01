using System;
using System.Numerics;
using System.Security.Cryptography;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using MithrilShards.Core.Crypto;
using MithrilShards.Network.Benchmark.Benchmarks;

namespace MithrilShards.Network.Benchmark {
   class Program {

      public class MyConfig : ManualConfig {
         public MyConfig() {
            //this.Add(CsvMeasurementsExporter.Default);
            //this.Add(RPlotExporter.Default);
            //this.Add(Job.Default
            //   .With(new GcMode() {
            //   Force = false // tell BenchmarkDotNet not to force GC collections after every iteration
            //}));
         }
      }

      static void Main(string[] args) {
         byte[] data = new byte[32];
         new Random().NextBytes(data);

         Console.WriteLine("MithrilShards.UInt256: " + new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UInt256(data).ToString());
         Console.WriteLine("MithrilShards.UInt256As4Long: " + new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UInt256As4Long(data).ToString());
         Console.WriteLine("NBitcoin: " + new NBitcoin.uint256(new ReadOnlySpan<byte>(data)).ToString());
         Console.WriteLine("NEO: " + new MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Neo.UInt256(new ReadOnlySpan<byte>(data)).ToString());
         Console.WriteLine("MithrilShards.UnsafeUInt256: " + new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UnsafeUInt256(data).ToString());
         Console.WriteLine("MithrilShards.UnsafeUInt256As4Long: " + new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UnsafeUInt256As4Long(data).ToString());
         Console.WriteLine("MithrilShards.UInt256As4Jhon: " + new P2P.Benchmark.Benchmarks.DataTypes.MithrilShards.UInt256As4Jhon(data).ToString());
         Console.WriteLine("MithrilShards.Core.DataTypes.UInt256: " + new MithrilShards.Core.DataTypes.UInt256(data).ToString());

         Console.WriteLine("MithrilShards.Core.DataTypes.UInt256: " + new MithrilShards.Core.DataTypes.UInt256("0123456789abcdef0123456789ABCDEF0123456789abcdef0123456789ABCDEF").ToString());

         BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
      }
   }
}

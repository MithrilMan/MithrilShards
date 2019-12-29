using System;
using System.Net;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MithrilShards.Core.Crypto;

namespace MithrilShards.Network.Benchmark.Benchmarks {
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   //[RPlotExporter, CsvMeasurementsExporter]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class IsRoutable {

      private IPAddress[] data;

      [Params(200)]
      public int N;

      [GlobalSetup]
      public void Setup() {
         var random = new Random();
         this.data = new IPAddress[this.N];
         for (int i = 0; i < this.N; i++) {
            byte[] randomIP = new byte[4];
            random.NextBytes(randomIP);
            this.data[i] = new IPAddress(randomIP);
         }
      }


      [Benchmark]
      public void NBitcoin_IsRoutable() {
         for (int i = 0; i < this.N; i++) {
            NBitcoin.IpExtensions.IsRoutable(this.data[i], true);
         }
      }

      [Benchmark]
      public void MithrilShards_IsRoutable() {
         for (int i = 0; i < this.N; i++) {
            MithrilShards.Core.Extensions.IPAddressExtensions.IsRoutable(this.data[i], true);
         }
      }
   }
}

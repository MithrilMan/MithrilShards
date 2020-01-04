using System;
using System.Net;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class IsRoutable
   {

      private IPAddress data;

      [GlobalSetup]
      public void Setup()
      {
         var random = new Random();
         byte[] randomIP = new byte[4];
         random.NextBytes(randomIP);
         this.data = new IPAddress(randomIP);
      }

      [Benchmark]
      public void NBitcoin_IsRoutable()
      {
         NBitcoin.IpExtensions.IsRoutable(this.data, true);
      }

      [Benchmark]
      public void MithrilShards_IsRoutable()
      {
         MithrilShards.Core.Extensions.IPAddressExtensions.IsRoutable(this.data, true);
      }
   }
}

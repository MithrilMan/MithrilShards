using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin;

namespace MithrilShards.Network.Benchmark.Benchmarks;

[SimpleJob(RuntimeMoniker.NetCoreApp31)]
[RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
public class AllocationSize
{
   [Params(1, 100, 1000)]
   public int size;

   [GlobalSetup]
   public void Setup()
   {
   }

   [Benchmark]
   public NBitcoin.BlockHeader[] HeadersNoInstances() => new NBitcoin.BlockHeader[size];

   [Benchmark]
   public NBitcoin.Target[] TargetsNoInstances() => new NBitcoin.Target[size];

   [Benchmark]
   public NBitcoin.BlockHeader[] HeadersWithInstances() => CreateHeaders(size);

   private BlockHeader[] CreateHeaders(int size)
   {
      var array = new NBitcoin.BlockHeader[this.size];
      for (int i = 0; i < array.Length; i++)
      {
#pragma warning disable CS0618 // Type or member is obsolete
         array[i] = new BlockHeader();
#pragma warning restore CS0618 // Type or member is obsolete
      }

      return array;
   }

   [Benchmark]
   public NBitcoin.Target[] TargetsWithInstances() => CreateTargets(size);

   private NBitcoin.Target[] CreateTargets(int size)
   {
      var array = new NBitcoin.Target[this.size];
      for (int i = 0; i < array.Length; i++)
      {
         array[i] = new NBitcoin.Target(1);
      }

      return array;
   }
}

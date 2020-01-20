using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class ComputeBlockHash
   {
      [Params(100, 1_000, 2_000)]
      public int N;

      BlockHeaderHashCalculator hashCalculator;

      BlockHeader[] headers;

      [GlobalSetup]
      public void Setup()
      {
         this.headers = Enumerable.Range(0, N)
            .Select(n => new BlockHeader
            {
               PreviousBlockHash = Core.DataTypes.UInt256.Zero,
               MerkleRoot = Core.DataTypes.UInt256.Zero
            })
            .ToArray();
      }

      [Benchmark]
      public object Sequential()
      {
         foreach (var header in headers)
         {
            header.Hash = this.hashCalculator.ComputeHash(header, KnownVersion.CurrentVersion);
         }
         return null;
      }

      [Benchmark]
      public object Parallelized()
      {
         return Parallel.ForEach(headers, header => header.Hash = this.hashCalculator.ComputeHash(header, KnownVersion.CurrentVersion));
      }
   }
}

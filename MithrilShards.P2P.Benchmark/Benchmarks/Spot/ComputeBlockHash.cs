using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Network.Benchmark.Benchmarks
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser]
   public class ComputeBlockHash
   {
      [Params(100, 1_000, 2_000)]
      public int N;

      BlockHeaderHashCalculator hashCalculator = new BlockHeaderHashCalculator(new BlockHeaderSerializer(new UInt256Serializer()));

      BlockHeader[] headers;

      NBitcoin.BlockHeader[] headersNBitcoin;

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

         this.headersNBitcoin = Enumerable.Range(0, N)
#pragma warning disable CS0618 // Type or member is obsolete
           .Select(n => new NBitcoin.BlockHeader()
           {
              HashPrevBlock = new NBitcoin.uint256(),
              HashMerkleRoot = new NBitcoin.uint256()
           })
#pragma warning restore CS0618 // Type or member is obsolete
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

      [Benchmark]
      public object SequentialNBitcoin()
      {
         foreach (var header in headersNBitcoin)
         {
            header.GetHash();
         }
         return null;
      }

      [Benchmark]
      public object ParallelizedNBitcoin()
      {
         return Parallel.ForEach(headersNBitcoin, header => header.GetHash());
      }
   }
}

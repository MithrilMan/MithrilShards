using System.Buffers;
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

      BlockHeaderHashCalculator _hashCalculator = new(new BlockHeaderSerializer(new UInt256Serializer()));

      BlockHeader[] _headers;

      NBitcoin.BlockHeader[] _headersNBitcoin;

      [GlobalSetup]
      public void Setup()
      {
         _headers = Enumerable.Range(0, N)
            .Select(n => new BlockHeader
            {
               PreviousBlockHash = Core.DataTypes.UInt256.Zero,
               MerkleRoot = Core.DataTypes.UInt256.Zero
            })
            .ToArray();

         _headersNBitcoin = Enumerable.Range(0, N)
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
         foreach (var header in _headers)
         {
            header.Hash = _hashCalculator.ComputeHash(header, KnownVersion.CurrentVersion);
         }
         return null;
      }

      [Benchmark]
      public object Parallelized()
      {
         return Parallel.ForEach(_headers, header => header.Hash = _hashCalculator.ComputeHash(header, KnownVersion.CurrentVersion));
      }

      [Benchmark]
      public object SequentialNBitcoin()
      {
         foreach (var header in _headersNBitcoin)
         {
            header.GetHash();
         }
         return null;
      }

      [Benchmark]
      public object ParallelizedNBitcoin()
      {
         return Parallel.ForEach(_headersNBitcoin, header => header.GetHash());
      }
   }
}

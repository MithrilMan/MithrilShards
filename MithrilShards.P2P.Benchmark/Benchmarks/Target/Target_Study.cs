using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using MithrilShards.Chain.Bitcoin.ChainDefinitions;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MS_Target = MithrilShards.Chain.Bitcoin.DataTypes.Target;
using NBitcoin_Target = NBitcoin.Target;

namespace MithrilShards.Network.Benchmark.Benchmarks.Target
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser, PlainExporter]
   public class Target_Study
   {
      //uint actualTimespan = 0x1b0404cb;
      uint _perfectCompactValue = 0x03123456;

      MS_Target _t1;
      NBitcoin_Target _nt1;

      //[Params(1, 1000, 1209600)] // 1209600 = actualTimespan value of bitcoin
      public uint scalar = (uint)TimeSpan.FromDays(14).TotalSeconds; //actualTimespan value of bitcoin

      BlockHeader _header;

      uint _lastRetargetTime = 1261130161;
      uint _blockTime = 1262152739;
      uint _bits = 0x1d00ffff;

      ProofOfWorkCalculator _powCalculator = new ProofOfWorkCalculator(
         new NullLogger<ProofOfWorkCalculator>(),
         new BitcoinMainDefinition(new BlockHeaderHashCalculator(new BlockHeaderSerializer(new UInt256Serializer()))).ConfigureConsensus(),
         null
         );

      NBitcoin.Consensus _nbitcoin_consensus = NBitcoin.Network.Main.Consensus;
      NBitcoin.Consensus _ms_consensus = NBitcoin.Network.Main.Consensus;

      [GlobalSetup]
      public void Setup()
      {
         _t1 = new MS_Target(_perfectCompactValue);
         _nt1 = new NBitcoin_Target(_perfectCompactValue);
         _header = new BlockHeader
         {
            TimeStamp = _blockTime,
            Bits = _bits
         };
      }

      //[Benchmark]
      //public uint ToCompact()
      //{
      //   return t1.ToCompact();
      //}

      //[Benchmark]
      //public uint ToCompact_NBitcoin()
      //{
      //   return nt1.ToCompact();
      //}

      //[Benchmark]
      //public MS_Target SetCompact()
      //{
      //   return new MS_Target(scalar);
      //}

      //[Benchmark]
      //public NBitcoin_Target SetCompact_NBitcoin()
      //{
      //   return new NBitcoin_Target(scalar);
      //}

      [Benchmark]
      public uint CalculateNextWorkRequired()
      {
         return _powCalculator.CalculateNextWorkRequired(_header, _lastRetargetTime);
      }

      [Benchmark]
      public uint CalculateNextWorkRequired_NBitcoin()
      {
         return NBitcoinStyle_CalculateNextWorkRequired(_header, _lastRetargetTime);
      }

      public uint NBitcoinStyle_CalculateNextWorkRequired(BlockHeader header, uint lastRetargetTime)
      {
         if (_nbitcoin_consensus.PowNoRetargeting)
            return header.Bits;

         // Limit adjustment step
         var nActualTimespan = TimeSpan.FromSeconds(header.TimeStamp - lastRetargetTime);
         if (nActualTimespan < TimeSpan.FromTicks(_nbitcoin_consensus.PowTargetTimespan.Ticks / 4))
            nActualTimespan = TimeSpan.FromTicks(_nbitcoin_consensus.PowTargetTimespan.Ticks / 4);
         if (nActualTimespan > TimeSpan.FromTicks(_nbitcoin_consensus.PowTargetTimespan.Ticks * 4))
            nActualTimespan = TimeSpan.FromTicks(_nbitcoin_consensus.PowTargetTimespan.Ticks * 4);

         // Retarget
         var bnNew = new NBitcoin_Target(header.Bits).ToBigInteger();
         var cmp = new NBitcoin_Target(bnNew).ToCompact();
         bnNew = bnNew * (new BigInteger((long)nActualTimespan.TotalSeconds));
         bnNew = bnNew / (new BigInteger((long)_nbitcoin_consensus.PowTargetTimespan.TotalSeconds));
         var newTarget = new NBitcoin_Target(bnNew);
         if (newTarget > _nbitcoin_consensus.PowLimit)
            newTarget = _nbitcoin_consensus.PowLimit;

         return newTarget.ToCompact();
      }
   }
}
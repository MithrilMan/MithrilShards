using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NBitcoin_Target = NBitcoin.Target;
using MS_Target = MithrilShards.Chain.Bitcoin.DataTypes.Target;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using System.Numerics;
using MithrilShards.Chain.Bitcoin.Consensus;
using BenchmarkDotNet.Loggers;
using Microsoft.Extensions.Logging.Abstractions;
using MithrilShards.Chain.Bitcoin.ChainDefinitions;
using Perfolizer.Mathematics.Randomization;
using System.Security.Cryptography;

namespace MithrilShards.Network.Benchmark.Benchmarks.Target
{
   [SimpleJob(RuntimeMoniker.NetCoreApp31)]
   [RankColumn, MarkdownExporterAttribute.GitHub, MemoryDiagnoser, PlainExporter]
   public class Target_Study
   {
      //uint actualTimespan = 0x1b0404cb;
      uint perfectCompactValue = 0x03123456;

      MS_Target t1;
      NBitcoin_Target nt1;

      //[Params(1, 1000, 1209600)] // 1209600 = actualTimespan value of bitcoin
      public uint scalar = (uint)TimeSpan.FromDays(14).TotalSeconds; //actualTimespan value of bitcoin

      BlockHeader header;

      uint lastRetargetTime = 1261130161;
      uint blockTime = 1262152739;
      uint bits = 0x1d00ffff;

      ProofOfWorkCalculator powCalculator = new ProofOfWorkCalculator(
         new NullLogger<ProofOfWorkCalculator>(),
         new BitcoinMainDefinition().ConfigureConsensus(),
         null
         );

      NBitcoin.Consensus nbitcoin_consensus = NBitcoin.Network.Main.Consensus;
      NBitcoin.Consensus ms_consensus = NBitcoin.Network.Main.Consensus;

      [GlobalSetup]
      public void Setup()
      {
         t1 = new MS_Target(perfectCompactValue);
         nt1 = new NBitcoin_Target(perfectCompactValue);
         header = new BlockHeader
         {
            TimeStamp = blockTime,
            Bits = bits
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
         return powCalculator.CalculateNextWorkRequired(header, lastRetargetTime);
      }

      [Benchmark]
      public uint CalculateNextWorkRequired_NBitcoin()
      {
         return NBitcoinStyle_CalculateNextWorkRequired(header, lastRetargetTime);
      }

      public uint NBitcoinStyle_CalculateNextWorkRequired(BlockHeader header, uint lastRetargetTime)
      {
         if (nbitcoin_consensus.PowNoRetargeting)
            return header.Bits;

         // Limit adjustment step
         TimeSpan nActualTimespan = TimeSpan.FromSeconds(header.TimeStamp - lastRetargetTime);
         if (nActualTimespan < TimeSpan.FromTicks(nbitcoin_consensus.PowTargetTimespan.Ticks / 4))
            nActualTimespan = TimeSpan.FromTicks(nbitcoin_consensus.PowTargetTimespan.Ticks / 4);
         if (nActualTimespan > TimeSpan.FromTicks(nbitcoin_consensus.PowTargetTimespan.Ticks * 4))
            nActualTimespan = TimeSpan.FromTicks(nbitcoin_consensus.PowTargetTimespan.Ticks * 4);

         // Retarget
         BigInteger bnNew = new NBitcoin_Target(header.Bits).ToBigInteger();
         var cmp = new NBitcoin_Target(bnNew).ToCompact();
         bnNew = bnNew * (new BigInteger((long)nActualTimespan.TotalSeconds));
         bnNew = bnNew / (new BigInteger((long)nbitcoin_consensus.PowTargetTimespan.TotalSeconds));
         var newTarget = new NBitcoin_Target(bnNew);
         if (newTarget > nbitcoin_consensus.PowLimit)
            newTarget = nbitcoin_consensus.PowLimit;

         return newTarget.ToCompact();
      }
   }
}
using Xunit;
using Xunit.Abstractions;
using MithrilShards.Chain.Bitcoin.ChainDefinitions;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Chain.Bitcoin.Consensus;
using System;
using System.Numerics;

namespace MithrilShards.Chain.BitcoinTests
{
   public class ProofOfWorkCalculatorTests
   {
      private XunitLogger<ProofOfWorkCalculator> logger;
      private IConsensusParameters consensusParameters;

      public ProofOfWorkCalculatorTests(ITestOutputHelper output)
      {
         logger = new XunitLogger<ProofOfWorkCalculator>(output); // or new NullLogger<ProofOfWorkCalculator>
         consensusParameters = new BitcoinMainDefinition().ConfigureConsensus();
      }

      public static TheoryData<uint, int, uint, uint, uint> Data() => new TheoryData<uint, int, uint, uint, uint>
      {
         { 1261130161, 32255, 1262152739, 0x1d00ffff, 0x1d00d86aU }, // Block #30240
         { 1231006505, 2015, 1233061996, 0x1d00ffff, 0x1d00ffffU }, // Block #0
         { 1279008237, 68543, 1279297671, 0x1c05a3f4, 0x1c0168fdU }, // Block #66528
         { 1279008237, 46367, 1269211443, 0x1c387f6f, 0x1d00e1fdU } // NOTE: Not an actual block time. Block 46367
      };


      [Theory]
      //[JsonFileData("_data/ProofOfWorkCalculatorTests.json", "CalculateNextWorkRequired")]
      [MemberData(nameof(Data))]
      public void CalculateNextWorkRequiredTest(uint lastRetargetTime, int height, uint blockTime, uint bits, uint expectedResult)
      {
         ProofOfWorkCalculator powCalculator = new ProofOfWorkCalculator(
            logger,
            consensusParameters,
            null
            );

         var header = new BlockHeader
         {
            TimeStamp = blockTime,
            Bits = bits
         };

         var result = powCalculator.CalculateNextWorkRequired(header, lastRetargetTime);
         Assert.Equal(expectedResult, result);
      }

      [Theory]
      [MemberData(nameof(Data))]
      public void NBitcoinCalculateNextWorkRequiredTest(uint lastRetargetTime, int height, uint blockTime, uint bits, uint expectedResult)
      {
         var header = new BlockHeader
         {
            TimeStamp = blockTime,
            Bits = bits
         };

         var result = NBitcoinCalculateNextWorkRequired(header, lastRetargetTime);
         Assert.Equal(expectedResult, result.ToCompact());
      }

      public NBitcoin.Target NBitcoinCalculateNextWorkRequired(BlockHeader header, uint lastRetargetTime)
      {
         var consensus = NBitcoin.Network.Main.Consensus;

         // Limit adjustment step
         TimeSpan nActualTimespan = TimeSpan.FromSeconds(header.TimeStamp - lastRetargetTime);
         if (nActualTimespan < TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks / 4))
            nActualTimespan = TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks / 4);
         if (nActualTimespan > TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks * 4))
            nActualTimespan = TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks * 4);

         // Retarget
         BigInteger bnNew = new NBitcoin.Target(header.Bits).ToBigInteger();
         var cmp = new NBitcoin.Target(bnNew).ToCompact();
         bnNew = bnNew * (new BigInteger((long)nActualTimespan.TotalSeconds));
         bnNew = bnNew / (new BigInteger((long)consensus.PowTargetTimespan.TotalSeconds));
         var newTarget = new NBitcoin.Target(bnNew);
         if (newTarget > consensus.PowLimit)
            newTarget = consensus.PowLimit;

         return newTarget;
      }
   }
}
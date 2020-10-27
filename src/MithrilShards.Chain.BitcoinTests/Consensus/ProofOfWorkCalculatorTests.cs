using Xunit;
using Xunit.Abstractions;
using MithrilShards.Chain.Bitcoin.ChainDefinitions;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Chain.Bitcoin.Consensus;
using System;
using System.Numerics;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;

namespace MithrilShards.Chain.BitcoinTests
{
   public class ProofOfWorkCalculatorTests
   {
      private XunitLogger<ProofOfWorkCalculator> logger;
      private IConsensusParameters consensusParameters;

      public ProofOfWorkCalculatorTests(ITestOutputHelper output)
      {
         logger = new XunitLogger<ProofOfWorkCalculator>(output); // or new NullLogger<ProofOfWorkCalculator>

         var headerHashCalculator = new BlockHeaderHashCalculator(new BlockHeaderSerializer(new UInt256Serializer()));
         consensusParameters = new BitcoinMainDefinition(headerHashCalculator).ConfigureConsensus();
      }

      [Theory]
      [JsonFileData("_data/ProofOfWorkCalculatorTests.json", "CalculateNextWorkRequired")]
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
      [JsonFileData("_data/ProofOfWorkCalculatorTests.json", "CalculateNextWorkRequired")]
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
using System;
using System.Numerics;
using MithrilShards.Chain.Bitcoin.ChainDefinitions;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using Xunit;
using Xunit.Abstractions;

namespace MithrilShards.Chain.BitcoinTests;

public class ProofOfWorkCalculatorTests
{
   private readonly XunitLogger<ProofOfWorkCalculator> _logger;
   private readonly IConsensusParameters _consensusParameters;

   public ProofOfWorkCalculatorTests(ITestOutputHelper output)
   {
      _logger = new XunitLogger<ProofOfWorkCalculator>(output); // or new NullLogger<ProofOfWorkCalculator>

      var headerHashCalculator = new BlockHeaderHashCalculator(new BlockHeaderSerializer(new UInt256Serializer()));
      _consensusParameters = new BitcoinMainDefinition(headerHashCalculator).ConfigureConsensus();
   }

   [Theory]
   [JsonFileData("_data/ProofOfWorkCalculatorTests.json", "CalculateNextWorkRequired")]
   public void CalculateNextWorkRequiredTest(uint lastRetargetTime, uint blockTime, uint bits, uint expectedResult)
   {
      var powCalculator = new ProofOfWorkCalculator(
         _logger,
         _consensusParameters,
         null
         );

      var header = new BlockHeader
      {
         TimeStamp = blockTime,
         Bits = bits
      };

      uint result = powCalculator.CalculateNextWorkRequired(header, lastRetargetTime);
      Assert.Equal(expectedResult, result);
   }

   [Theory]
   [JsonFileData("_data/ProofOfWorkCalculatorTests.json", "CalculateNextWorkRequired")]
   public void NBitcoinCalculateNextWorkRequiredTest(uint lastRetargetTime, uint blockTime, uint bits, uint expectedResult)
   {
      var header = new BlockHeader
      {
         TimeStamp = blockTime,
         Bits = bits
      };

      NBitcoin.Target result = NBitcoinCalculateNextWorkRequired(header, lastRetargetTime);
      Assert.Equal(expectedResult, result.ToCompact());
   }

   public NBitcoin.Target NBitcoinCalculateNextWorkRequired(BlockHeader header, uint lastRetargetTime)
   {
      NBitcoin.Consensus consensus = NBitcoin.Network.Main.Consensus;

      // Limit adjustment step
      var nActualTimespan = TimeSpan.FromSeconds(header.TimeStamp - lastRetargetTime);
      if (nActualTimespan < TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks / 4))
         nActualTimespan = TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks / 4);
      if (nActualTimespan > TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks * 4))
         nActualTimespan = TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks * 4);

      // Retarget
      var bnNew = new NBitcoin.Target(header.Bits).ToBigInteger();
      uint cmp = new NBitcoin.Target(bnNew).ToCompact();
      bnNew *= (new BigInteger((long)nActualTimespan.TotalSeconds));
      bnNew /= (new BigInteger((long)consensus.PowTargetTimespan.TotalSeconds));
      var newTarget = new NBitcoin.Target(bnNew);
      if (newTarget > consensus.PowLimit)
         newTarget = consensus.PowLimit;

      return newTarget;
   }
}

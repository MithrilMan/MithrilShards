using System;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{
   public class BitcoinRegtestDefinition : BitcoinChain
   {
      public BitcoinRegtestDefinition(IBlockHeaderHashCalculator blockHeaderHashCalculator) : base(blockHeaderHashCalculator) { }

      public override BitcoinNetworkDefinition ConfigureNetwork()
      {
         return new BitcoinNetworkDefinition
         {
            Name = "Bitcoin Regtest",
            Magic = 0xDAB5BFFA,
            MagicBytes = BitConverter.GetBytes(0xDAB5BFFA),
            DefaultMaxPayloadSize = 32_000_000
         };
      }

      public override ConsensusParameters ConfigureConsensus()
      {
         return new ConsensusParameters(
            genesisHeader: this.BuildGenesisBlock(),

            subsidyHalvingInterval: 150,
            maxMoney: 21_000_000 * COIN,

            powLimit: new Target("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
            powTargetTimespan: (uint)TimeSpan.FromDays(14).TotalSeconds, // 2 weeks
            powTargetSpacing: (uint)TimeSpan.FromMinutes(10).TotalSeconds,
            powAllowMinDifficultyBlocks: true,
            powNoRetargeting: true,

            minimumChainWork: Target.Zero,

            maxBlockSerializedSize: 4_000_000,
            witnessScaleFactor: 4,
            segwitHeight: 0 // SEGWIT is always activated on regtest unless overridden
         );
      }

      private BlockHeader BuildGenesisBlock()
      {
         //TODO complete construction (a Block will be needed and not a BlockHeader)
         var genesisHeader = new BlockHeader
         {
            Bits = 0x207fffff,
            TimeStamp = 1296688602,
            Nonce = 2,
            Version = 1,
            PreviousBlockHash = null
         };

         genesisHeader.Hash = new UInt256("0f9188f13cb7b2c71f2a335e3a4fc328bf5beb436012afca590b1a11466e2206");
         //this.ComputeHash(genesisHeader);

         return genesisHeader;
      }
   }
}

using System;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{
   public class BitcoinRegtestDefinition : BitcoinChain
   {
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
         BlockHeader genesisBlock = this.BuildGenesisBlock("0f9188f13cb7b2c71f2a335e3a4fc328bf5beb436012afca590b1a11466e2206");

         return new ConsensusParameters
         {
            Genesis = genesisBlock.Hash!,
            GenesisHeader = genesisBlock,

            PowLimit = new Target("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
            PowTargetTimespan = (uint)TimeSpan.FromDays(14).TotalSeconds, // 2 weeks
            PowTargetSpacing = (uint)TimeSpan.FromMinutes(10).TotalSeconds,
            PowAllowMinDifficultyBlocks = true,
            PowNoRetargeting = true,

            SubsidyHalvingInterval = 150,
            SegwitHeight = 0, // SEGWIT is always activated on regtest unless overridden
            MinimumChainWork = UInt256.Zero,
         };
      }

      private BlockHeader BuildGenesisBlock(string genesisHash)
      {
         //TODO complete construction (a Block will be needed and not a BlockHeader)
         return new BlockHeader
         {
            Bits = 0,
            Hash = new UInt256(genesisHash),
         };
      }
   }
}

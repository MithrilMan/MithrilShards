using System;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Crypto;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{
   public class BitcoinMainDefinition : BitcoinChain
   {
      public BitcoinMainDefinition(IBlockHeaderHashCalculator blockHeaderHashCalculator) : base(blockHeaderHashCalculator) { }

      public override BitcoinNetworkDefinition ConfigureNetwork()
      {
         return new BitcoinNetworkDefinition
         {
            Name = "Bitcoin Main",
            Magic = 0xD9B4BEF9,
            MagicBytes = BitConverter.GetBytes(0xD9B4BEF9),
            DefaultMaxPayloadSize = 32_000_000
         };
      }

      public override ConsensusParameters ConfigureConsensus()
      {
         return new ConsensusParameters
         {
            GenesisHeader = this.BuildGenesisBlock(),

            PowLimit = new Target("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
            PowTargetTimespan = (uint)TimeSpan.FromDays(14).TotalSeconds, // 2 weeks
            PowTargetSpacing = (uint)TimeSpan.FromMinutes(10).TotalSeconds,
            PowAllowMinDifficultyBlocks = false,
            PowNoRetargeting = false,

            SubsidyHalvingInterval = 210000,
            SegwitHeight = 481824, // 0000000000000000001c8018d9cb3b742ef25114f27563e3fc4a1902167f9893,
            MinimumChainWork = new Target("0x000000000000000000000000000000000000000008ea3cf107ae0dec57f03fe8"),
         };
      }

      private BlockHeader BuildGenesisBlock()
      {
         //TODO complete construction (a Block will be needed and not a BlockHeader)
         var genesisHeader = new BlockHeader
         {
            Bits = 0x1d00ffff,
            TimeStamp = 1231006505,
            Nonce = 2083236893,
            Version = 1,
            PreviousBlockHash = null
         };

         genesisHeader.Hash = new UInt256("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f");
         //this.ComputeHash(genesisHeader);

         return genesisHeader;
      }
   }
}

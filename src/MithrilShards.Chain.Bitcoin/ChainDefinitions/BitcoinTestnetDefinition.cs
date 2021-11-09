using System;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions;

public class BitcoinTestnetDefinition : BitcoinChain
{
   public BitcoinTestnetDefinition(IBlockHeaderHashCalculator blockHeaderHashCalculator) : base(blockHeaderHashCalculator) { }

   public override BitcoinNetworkDefinition ConfigureNetwork()
   {
      return new BitcoinNetworkDefinition
      {
         Name = "Bitcoin Testnet",
         Magic = 0x0709110B,
         MagicBytes = BitConverter.GetBytes(0x0709110B),
         DefaultMaxPayloadSize = 32_000_000
      };
   }

   public override ConsensusParameters ConfigureConsensus()
   {
      return new ConsensusParameters(
         genesisHeader: BuildGenesisBlock(),

         subsidyHalvingInterval: 210000,
         maxMoney: 21_000_000 * COIN,

         powLimit: new Target("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
         powTargetTimespan: (uint)TimeSpan.FromDays(14).TotalSeconds, // 2 weeks
         powTargetSpacing: (uint)TimeSpan.FromMinutes(10).TotalSeconds,
         powAllowMinDifficultyBlocks: true,
         powNoRetargeting: false,

         minimumChainWork: new Target("0x00000000000000000000000000000000000000000000012b2a3a62424f21c918"),

         maxBlockSerializedSize: 4_000_000,
         witnessScaleFactor: 4,
         segwitHeight: 834624 // 00000000002b980fcd729daaa248fd9316a5200e9b367f4ff2c42453e84201ca
      );
   }

   private BlockHeader BuildGenesisBlock()
   {
      //TODO complete construction (a Block will be needed and not a BlockHeader)
      var genesisHeader = new BlockHeader
      {
         Bits = 0x1d00ffff,
         TimeStamp = 1296688602,
         Nonce = 414098458,
         Version = 1,
         PreviousBlockHash = null
      };

      genesisHeader.Hash = new UInt256("000000000933ea01ad0ee984209779baaec3ced90fa3f408719526f8d77f4943");
      //this.ComputeHash(genesisHeader);

      return genesisHeader;
   }
}

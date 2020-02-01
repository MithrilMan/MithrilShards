using System;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{
   public class BitcoinTestnetDefinition : BitcoinChain
   {
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
         return new ConsensusParameters
         {
            Genesis = new UInt256("000000000933ea01ad0ee984209779baaec3ced90fa3f408719526f8d77f4943"),
            PowTargetSpacing = (long)TimeSpan.FromMinutes(10).TotalSeconds,
            PowLimit = new UInt256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
            PowTargetTimespan = (long)TimeSpan.FromDays(14).TotalSeconds, // 2 weeks
            SubsidyHalvingInterval = 210000,
            SegwitHeight = 834624, // 00000000002b980fcd729daaa248fd9316a5200e9b367f4ff2c42453e84201ca
            MinimumChainWork = new UInt256("0x00000000000000000000000000000000000000000000012b2a3a62424f21c918"),
         };
      }
   }
}

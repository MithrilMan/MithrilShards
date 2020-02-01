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
         };
      }
   }
}

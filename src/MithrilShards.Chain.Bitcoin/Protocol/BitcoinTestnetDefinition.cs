using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;
using System;

namespace MithrilShards.Chain.Bitcoin.Protocol {
   public class BitcoinTestnetDefinition : IChainDefinition {
      public string Name { get; }

      public byte[] MagicBytes { get; }

      public UInt256 Genesis { get; }

      public uint Magic { get; }

      public int DefaultMaxPayloadSize { get; }

      public BitcoinTestnetDefinition() {
         this.Name = "BitcoinMain";
         this.Magic = 0xDAB5BFFA;
         this.MagicBytes = BitConverter.GetBytes(0xDAB5BFFA);
         this.Genesis = null;
         this.DefaultMaxPayloadSize = 32_000_000;
      }
   }
}

using MithrilShards.Core.Network.Protocol;
using System;

namespace MithrilShards.Chain.Bitcoin.Protocol {
   public class BitcoinMainDefinition : IChainDefinition {
      public string Name { get; }

      public byte[] MagicBytes { get; }

      public byte[] genesis { get; }

      public BitcoinMainDefinition() {
         this.Name = "BitcoinMain";
         this.MagicBytes = BitConverter.GetBytes(0xD9B4BEF9);
         this.genesis = null;
      }
   }
}

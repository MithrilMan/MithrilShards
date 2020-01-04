using System;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   public class BitcoinMainDefinition : IChainDefinition
   {
      public string Name { get; }

      public byte[] MagicBytes { get; }

      public UInt256 Genesis { get; }

      public uint Magic { get; }

      public int DefaultMaxPayloadSize { get; }

      public BitcoinMainDefinition()
      {
         this.Name = "Bitcoin Main";
         this.Magic = 0xD9B4BEF9;
         this.MagicBytes = BitConverter.GetBytes(0xD9B4BEF9);
         this.Genesis = null;
         this.DefaultMaxPayloadSize = 32_000_000;
      }
   }
}

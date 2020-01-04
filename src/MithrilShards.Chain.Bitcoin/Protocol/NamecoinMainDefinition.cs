using System;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   public class NamecoinMainDefinition : IChainDefinition
   {
      public string Name { get; }

      public byte[] MagicBytes { get; }

      public UInt256 Genesis { get; }

      public uint Magic { get; }

      public int DefaultMaxPayloadSize { get; }

      public NamecoinMainDefinition()
      {
         this.Name = "Namecoin Main";
         this.Magic = 0xFEB4BEF9;
         this.MagicBytes = BitConverter.GetBytes(0xFEB4BEF9);
         this.Genesis = null;
         this.DefaultMaxPayloadSize = 32_000_000;
      }
   }
}

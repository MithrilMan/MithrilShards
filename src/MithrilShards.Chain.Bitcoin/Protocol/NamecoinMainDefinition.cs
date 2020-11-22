using System;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   public class NamecoinMainDefinition : INetworkDefinition
   {
      public string Name { get; }

      public byte[] MagicBytes { get; }

      public UInt256 Genesis { get; }

      public uint Magic { get; }

      public int DefaultMaxPayloadSize { get; }

      public NamecoinMainDefinition()
      {
         Name = "Namecoin Main";
         Magic = 0xFEB4BEF9;
         MagicBytes = BitConverter.GetBytes(0xFEB4BEF9);
         Genesis = new UInt256("000000000062b72c5e2ceb45fbc8587e807c155b0da735e6483dfba2f0a9c770");
         DefaultMaxPayloadSize = 32_000_000;
      }
   }
}

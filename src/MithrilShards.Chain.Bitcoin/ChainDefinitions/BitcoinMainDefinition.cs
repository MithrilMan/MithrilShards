using System;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
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
         this.Genesis = new UInt256("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f");
         this.DefaultMaxPayloadSize = 32_000_000;
      }
   }
}

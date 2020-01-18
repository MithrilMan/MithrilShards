using System;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{
   public class BitcoinTestnetDefinition : IChainDefinition
   {
      public string Name { get; }

      public byte[] MagicBytes { get; }

      public UInt256 Genesis { get; }

      public uint Magic { get; }

      public int DefaultMaxPayloadSize { get; }

      public BitcoinTestnetDefinition()
      {
         this.Name = "Bitcoin Testnet";
         this.Magic = 0xDAB5BFFA;
         this.MagicBytes = BitConverter.GetBytes(0x0709110B);
         this.Genesis = new UInt256("000000000933ea01ad0ee984209779baaec3ced90fa3f408719526f8d77f4943");
         this.DefaultMaxPayloadSize = 32_000_000;
      }
   }
}

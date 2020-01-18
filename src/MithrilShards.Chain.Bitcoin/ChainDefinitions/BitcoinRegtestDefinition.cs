using System;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{
   public class BitcoinRegtestDefinition : IChainDefinition
   {
      public string Name { get; }

      public byte[] MagicBytes { get; }

      public UInt256 Genesis { get; }

      public uint Magic { get; }

      public int DefaultMaxPayloadSize { get; }

      public BitcoinRegtestDefinition()
      {
         this.Name = "Bitcoin Regtest";
         this.Magic = 0xDAB5BFFA;
         this.MagicBytes = BitConverter.GetBytes(0xDAB5BFFA);
         this.Genesis = new UInt256("0f9188f13cb7b2c71f2a335e3a4fc328bf5beb436012afca590b1a11466e2206");
         this.DefaultMaxPayloadSize = 32_000_000;
      }
   }
}

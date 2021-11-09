using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Network;

public class BitcoinNetworkDefinition : INetworkDefinition
{
   public string Name { get; set; } = null!;

   public uint Magic { get; set; }

   public byte[] MagicBytes { get; set; } = null!;

   public int DefaultMaxPayloadSize { get; set; }
}

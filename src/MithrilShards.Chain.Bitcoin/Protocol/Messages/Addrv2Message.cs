using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   /// <summary>
   /// The 'addrv2' message relays connection information for peers, using the new
   /// variable-length address format defined in BIP155.
   /// </summary>
   [NetworkMessage(COMMAND)]
   public sealed class Addrv2Message : INetworkMessage
   {
      public const string COMMAND = "addrv2";
      string INetworkMessage.Command => COMMAND;

      /// <summary>
      /// The number of addresses in this message.
      /// Serialized as a CompactSize. The deserializer will populate this
      /// based on the number of items read into the Addresses list.
      /// The serializer will write it based on Addresses.Count.
      /// </summary>
      public uint Count => (uint)(Addresses?.Count ?? 0);

      /// <summary>
      /// A list of network addresses.
      /// Maximum number of items is 1000.
      /// </summary>
      public List<NetworkAddressV2> Addresses { get; set; } = new List<NetworkAddressV2>();
   }
}

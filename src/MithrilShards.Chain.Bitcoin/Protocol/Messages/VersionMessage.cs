using System;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;
using MithrilShards.Core.Network.Protocol.Serialization;
using Types = MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;


namespace MithrilShards.Chain.Bitcoin.Protocol.Messages {
   [NetworkMessage("version")]
   public class VersionMessage : NetworkMessage {

      /// <summary>
      /// Identifies protocol version being used by the node
      /// </summary>
      public int Version { get; set; }

      /// <summary>
      /// Bitfield of features to be enabled for this connection
      /// </summary>
      public ulong Services { get; set; }

      /// <summary>
      /// standard UNIX timestamp in seconds
      /// </summary>
      public DateTimeOffset Timestamp { get; set; }

      /// <summary>
      /// The network address of the node receiving this message (addr_recv)
      /// </summary>

      public NetworkAddress ReceiverAddress { get; set; }

      /// <summary>
      /// The network address of the node emitting this message (addr_from)
      /// </summary>
      public NetworkAddress SenderAddress { get; set; }

      /// <summary>
      /// Node random nonce, randomly generated every time a version packet is sent. This nonce is used to detect connections to self.
      /// </summary>
      public ulong Nonce { get; set; }

      /// <summary>
      /// User Agent (0x00 if string is 0 bytes long)
      /// </summary>
      public string UserAgent { get; set; }

      /// <summary>
      /// The last block received by the emitting node
      /// </summary>
      public int StartHeight { get; set; }

      /// <summary>
      /// Whether the remote peer should announce relayed transactions or not, see BIP 0037.
      /// </summary>
      public bool Relay { get; set; }

      public VersionMessage() : base("version") {
      }
   }
}
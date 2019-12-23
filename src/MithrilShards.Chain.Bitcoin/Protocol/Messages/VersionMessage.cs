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
      [NetworkMessageField(typeof(Types.Int), 1)]
      public int Version { get; set; }

      /// <summary>
      /// Bitfield of features to be enabled for this connection
      /// </summary>
      [NetworkMessageField(typeof(Types.ULong), 2)]
      public ulong Services { get; set; }

      /// <summary>
      /// standard UNIX timestamp in seconds
      /// </summary>
      [NetworkMessageField(typeof(Types.Long), 3)]
      public DateTimeOffset Timestamp { get; set; }

      /// <summary>
      /// The network address of the node receiving this message (addr_recv)
      /// </summary>
      [NetworkMessageField(typeof(Types.NetworkAddress), 4)]

      public NetworkAddress ReceiverAddress { get; set; }

      /// <summary>
      /// The network address of the node emitting this message (addr_from)
      /// </summary>
      [NetworkMessageField(typeof(Types.NetworkAddress), 5, minVersion: 106)]
      public NetworkAddress SenderAddress { get; set; }

      /// <summary>
      /// Node random nonce, randomly generated every time a version packet is sent. This nonce is used to detect connections to self.
      /// </summary>
      [NetworkMessageField(typeof(Types.ULong), 6, minVersion: 106)]
      public ulong Nonce { get; set; }

      /// <summary>
      /// User Agent (0x00 if string is 0 bytes long)
      /// </summary>
      [NetworkMessageField(typeof(Types.VarString), 7, minVersion: 106)]
      public string UserAgent { get; set; }

      /// <summary>
      /// The last block received by the emitting node
      /// </summary>
      [NetworkMessageField(typeof(Types.Int), 8, minVersion: 106)]
      public int StartHeight { get; set; }

      /// <summary>
      /// Whether the remote peer should announce relayed transactions or not, see BIP 0037.
      /// </summary>
      [NetworkMessageField(typeof(Types.NetworkAddress), 9, minVersion: 70001)]
      public bool Relay { get; set; }

      public VersionMessage() : base("version") {
      }
   }
}
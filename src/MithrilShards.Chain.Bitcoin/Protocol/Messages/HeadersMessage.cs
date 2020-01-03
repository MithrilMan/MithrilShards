using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages {
   /// <summary>
   /// The headers packet returns block headers in response to a getheaders packet.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Messages.NetworkMessage" />
   [NetworkMessage("headers")]
   public class HeadersMessage : NetworkMessage {

      public BlockHeader[] Headers { get; set; }

      public HeadersMessage() : base("headers") { }
   }
}
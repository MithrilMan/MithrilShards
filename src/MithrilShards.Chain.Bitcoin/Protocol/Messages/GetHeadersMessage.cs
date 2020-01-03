using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages {
   [NetworkMessage("getheaders")]
   public class GetHeadersMessage : NetworkMessage {

      /// <summary>
      /// The protocol version.
      /// </summary>
      public uint Version { get; set; }

      /// <summary>
      /// Block locator objects.
      /// Newest back to genesis block (dense to start, but then sparse)
      /// </summary>
      public BlockLocator BlockLocator { get; set; }

      /// <summary>
      /// Hash of the last desired block header
      /// Set to zero to get as many blocks as possible (2000 by default)
      /// </summary>
      public UInt256 HashStop { get; set; }

      public GetHeadersMessage() : base("getheaders") { }
   }
}
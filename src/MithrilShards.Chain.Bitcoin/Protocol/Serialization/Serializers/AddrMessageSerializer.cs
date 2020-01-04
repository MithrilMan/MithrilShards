using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers {
   public class AddrMessageSerializer : NetworkMessageSerializerBase<AddrMessage> {

      public AddrMessageSerializer(IChainDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(AddrMessage message, int protocolVersion, IBufferWriter<byte> output) {
         output.WriteArray(message.Addresses, protocolVersion);
      }

      public override AddrMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion) {
         return new AddrMessage {
            Addresses = reader.ReadArray<NetworkAddress>(protocolVersion)
         };
      }
   }
}

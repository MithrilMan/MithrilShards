using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class AddrMessageSerializer : NetworkMessageSerializerBase<AddrMessage>
   {
      private readonly IProtocolTypeSerializer<NetworkAddress> networkAddressSerializer;

      public AddrMessageSerializer(IChainDefinition chainDefinition, IProtocolTypeSerializer<NetworkAddress> networkAddressSerializer) : base(chainDefinition)
      {
         this.networkAddressSerializer = networkAddressSerializer;
      }

      public override void Serialize(AddrMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         output.WriteArray(message.Addresses!, protocolVersion, this.networkAddressSerializer);
      }

      public override AddrMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         return new AddrMessage { Addresses = reader.ReadArray(protocolVersion, this.networkAddressSerializer) };
      }
   }
}

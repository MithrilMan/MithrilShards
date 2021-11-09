using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages;

public class AddrMessageSerializer : BitcoinNetworkMessageSerializerBase<AddrMessage>
{
   private readonly IProtocolTypeSerializer<NetworkAddress> _networkAddressSerializer;

   public AddrMessageSerializer(IProtocolTypeSerializer<NetworkAddress> networkAddressSerializer)
   {
      _networkAddressSerializer = networkAddressSerializer;
   }

   public override void Serialize(AddrMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
   {
      output.WriteArray(message.Addresses!, protocolVersion, _networkAddressSerializer);
   }

   public override AddrMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
   {
      return new AddrMessage { Addresses = reader.ReadArray(protocolVersion, _networkAddressSerializer) };
   }
}

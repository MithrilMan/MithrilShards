using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;

public class NetworkAddressNoTimeSerializer : IProtocolTypeSerializer<NetworkAddressNoTime>
{
   public NetworkAddressNoTime Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null)
   {
      return new NetworkAddressNoTime { Services = reader.ReadULong(), IP = reader.ReadBytes(16).ToArray(), Port = reader.ReadUShort() };
   }

   public int Serialize(NetworkAddressNoTime typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
   {
      int size = 0;
      size += writer.WriteULong(typeInstance.Services);
      size += writer.WriteBytes(typeInstance.IP!);
      size += writer.WriteUShort(typeInstance.Port);

      return size;
   }
}

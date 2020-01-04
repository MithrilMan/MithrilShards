using System.Buffers;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   public interface INetworkMessageSerializerManager
   {
      bool TrySerialize(INetworkMessage message, int protocolVersion, IBufferWriter<byte> output, out int serializedLength);

      bool TryDeserialize(string commandName, ref ReadOnlySequence<byte> data, int protocolVersion, out INetworkMessage message);
   }
}
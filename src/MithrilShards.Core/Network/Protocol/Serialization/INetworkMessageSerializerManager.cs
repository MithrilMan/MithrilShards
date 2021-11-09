using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace MithrilShards.Core.Network.Protocol.Serialization;

public interface INetworkMessageSerializerManager
{
   bool TrySerialize(INetworkMessage message, int protocolVersion, IPeerContext peerContext, IBufferWriter<byte> output);

   bool TryDeserialize(string commandName, ref ReadOnlySequence<byte> data, int protocolVersion, IPeerContext peerContext, [MaybeNullWhen(false)] out INetworkMessage message);
}

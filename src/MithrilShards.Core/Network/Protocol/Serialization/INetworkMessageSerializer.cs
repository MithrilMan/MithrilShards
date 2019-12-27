using System;
using System.Buffers;

namespace MithrilShards.Core.Network.Protocol.Serialization {
   public interface INetworkMessageSerializer {
      /// <summary>
      /// Gets the type of the message managed by the serializer.
      /// </summary>
      /// <returns></returns>
      Type GetMessageType();

      byte[] Serialize(INetworkMessage message, int protocolVersion, IBufferWriter<byte> output);

      INetworkMessage Deserialize(ReadOnlySequence<byte> data, int protocolVersion);
   }
}

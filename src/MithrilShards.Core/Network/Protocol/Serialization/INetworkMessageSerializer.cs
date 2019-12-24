using System;
using System.Buffers;
using System.Collections.Generic;

namespace MithrilShards.Core.Network.Protocol.Serialization {
   public interface INetworkMessageSerializer {
      /// <summary>
      /// Gets the type of the message managed by the serializer.
      /// </summary>
      /// <returns></returns>
      Type GetMessageType();

      byte[] Serialize(INetworkMessage message);

      INetworkMessage Deserialize(ReadOnlySequence<byte> data);
   }

   public interface INetworkMessageSerializer<TMessageType> : INetworkMessageSerializer where TMessageType : INetworkMessage {
      byte[] Serialize(TMessageType message);

      TMessageType Deserialize(ReadOnlySequence<byte> data);
   }
}

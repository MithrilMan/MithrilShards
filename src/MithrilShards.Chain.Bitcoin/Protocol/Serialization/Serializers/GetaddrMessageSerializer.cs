using System;
using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers {
   public class GetaddrMessageSerializer : NetworkMessageSerializerBase<GetaddrMessage> {
      private static readonly GetaddrMessage instance = new GetaddrMessage();
      public override INetworkMessage Deserialize(ReadOnlySequence<byte> data, int protocolVersion) {
         return instance;
      }

      public override byte[] Serialize(GetaddrMessage message, int protocolVersion, IBufferWriter<byte> output) {
         return null;
      }
   }
}

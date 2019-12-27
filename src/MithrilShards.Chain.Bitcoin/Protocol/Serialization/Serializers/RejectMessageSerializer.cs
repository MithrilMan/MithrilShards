using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers {
   public class RejectMessageSerializer : NetworkMessageSerializerBase<RejectMessage> {
      public override INetworkMessage Deserialize(ReadOnlySequence<byte> data, int protocolVersion) {
         var reader = new SequenceReader<byte>(data);

         var message = new RejectMessage {
            Reason = reader.ReadVarString(),
            Code = (RejectMessage.RejectCode)reader.ReadByte()
         };

         if (reader.Remaining > 0) {
            message.Data = reader.ReadByte();
         }

         return message;
      }

      public override byte[] Serialize(RejectMessage message, int protocolVersion, IBufferWriter<byte> output) {
         return null;
      }
   }
}

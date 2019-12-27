using System;
using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers {
   public class RejectMessageSerializer : NetworkMessageSerializerBase<RejectMessage> {
      const int MAX_DATA_SIZE = 1_000; // usually it should contains 32 bytes, let be generous
      public RejectMessageSerializer(IChainDefinition chainDefinition) : base(chainDefinition) { }

      public override INetworkMessage Deserialize(ReadOnlySequence<byte> data, int protocolVersion) {
         var reader = new SequenceReader<byte>(data);

         var message = new RejectMessage {
            Reason = reader.ReadVarString(),
            Code = (RejectMessage.RejectCode)reader.ReadByte()
         };

         if (reader.Remaining > 0) {
            message.Data = reader.ReadBytes((int)Math.Min(reader.Remaining, MAX_DATA_SIZE));
         }

         return message;
      }

      public override void Serialize(RejectMessage message, int protocolVersion, IBufferWriter<byte> output) {
         output.WriteVarString(message.Reason);
         output.WriteByte((byte)message.Code);
         if ((message.Data?.Length ?? 0) > 0) {
            output.WriteBytes(message.Data);
         }
      }
   }
}

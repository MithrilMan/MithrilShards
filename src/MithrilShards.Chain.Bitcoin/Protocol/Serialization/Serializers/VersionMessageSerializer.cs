using System;
using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers {
   public class VersionMessageSerializer : NetworkMessageSerializerBase<VersionMessage> {
      public override byte[] Serialize(VersionMessage message, int protocolVersion, IBufferWriter<byte> output) {
         throw new NotImplementedException();
      }

      public override INetworkMessage Deserialize(ReadOnlySequence<byte> data, int protocolVersion) {
         var reader = new SequenceReader<byte>(data);

         var message = new VersionMessage {
            Version = reader.ReadInt(),
            Services = reader.ReadULong(),
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.ReadLong()),
            ReceiverAddress = reader.ReadNetworkAddress(skipTimeField: true)
         };

         if (message.Version < KnownVersion.V106) {
            return message;
         }

         message.SenderAddress = reader.ReadNetworkAddress(skipTimeField: true);
         message.Nonce = reader.ReadULong();
         message.UserAgent = reader.ReadVarString();
         message.StartHeight = reader.ReadInt();

         if (message.Version < KnownVersion.V70001) {
            return message;
         }

         message.Relay = reader.ReadBool();
         return message;
      }

   }
}

using System;
using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers {
   public class VersionMessageSerializer : NetworkMessageSerializerBase<VersionMessage> {
      public override byte[] Serialize(VersionMessage message) {
         throw new NotImplementedException();
      }

      public override VersionMessage Deserialize(ReadOnlySpan<byte> data) {
         var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data.ToArray()));

         var message = new VersionMessage();
         message.Version = reader.ReadInt();
         message.Services = reader.ReadULong();
         message.Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.ReadLong());

         message.ReceiverAddressServices = reader.ReadULong();
         message.ReceiverAddressIP = reader.ReadBytes(16);
         message.SenderAddressPort = reader.ReadUShort();

         // TODO: add Protocol Version enum ? 
         if (message.Version < 106) {
            return message;
         }

         message.SenderAddressServices = reader.ReadULong();
         message.SenderAddressIP = reader.ReadBytes(16);
         message.SenderAddressPort = reader.ReadUShort();

         message.Nonce = reader.ReadULong();
         message.UserAgent = reader.ReadVarString();
         message.StartHeight = reader.ReadInt();

         if (message.Version < 70001) {
            return message;
         }

         message.Relay = reader.ReadBool();
         return message;
      }
   }
}

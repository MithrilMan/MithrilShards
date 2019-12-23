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
         message.ReceiverAddress = reader.ReadNetworkAddress();
         // from here down, if version >= 106
         message.SenderAddress = reader.ReadNetworkAddress();
         message.Nonce = reader.ReadULong();
         message.UserAgent = reader.ReadVarString();
         message.StartHeight = reader.ReadInt();
         // from here down, if version >= 70001
         message.Relay = reader.ReadBool();
         return message;
      }
   }
}

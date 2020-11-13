using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Example.Network;
using MithrilShards.Example.Protocol.Messages;

namespace MithrilShards.Example.Protocol.Serialization.Serializers.Messages
{
   public class VersionMessageSerializer : ExampleNetworkMessageSerializerBase<VersionMessage>
   {
      public VersionMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(VersionMessage message, int protocolVersion, ExamplePeerContext peerContext, IBufferWriter<byte> output)
      {
         // outgoing version message doesn't have to look into passed protocolVersion but rely on it's message.Version.
         protocolVersion = message.Version;

         output.WriteInt(message.Version);
         output.WriteLong(message.Timestamp.ToUnixTimeSeconds());
         output.WriteULong(message.Nonce);

         if (protocolVersion < 3) return;

         output.WriteVarString(message.UserAgent!);
      }

      public override VersionMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ExamplePeerContext peerContext)
      {
         var message = new VersionMessage
         {
            Version = reader.ReadInt(),
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.ReadLong()),
            Nonce = reader.ReadULong(),
         };

         if (protocolVersion < 3) return message;

         message.UserAgent = reader.ReadVarString();

         return message;
      }
   }
}

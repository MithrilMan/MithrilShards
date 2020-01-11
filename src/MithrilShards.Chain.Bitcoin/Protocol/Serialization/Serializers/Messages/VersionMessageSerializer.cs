using System;
using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class VersionMessageSerializer : NetworkMessageSerializerBase<VersionMessage>
   {
      private readonly IProtocolTypeSerializer<NetworkAddressNoTime> networkAddressNoTimeSerializer;

      public VersionMessageSerializer(IChainDefinition chainDefinition, IProtocolTypeSerializer<NetworkAddressNoTime> networkAddressNoTimeSerializer) : base(chainDefinition)
      {
         this.networkAddressNoTimeSerializer = networkAddressNoTimeSerializer;
      }

      public override void Serialize(VersionMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         // version message doesn't have to look into passed protocolVersion but rely on it's message.Version.
         protocolVersion = message.Version;

         output.WriteInt(message.Version);
         output.WriteULong(message.Services);
         output.WriteLong(message.Timestamp.ToUnixTimeSeconds());
         output.WriteWithSerializer(message.ReceiverAddress, protocolVersion, this.networkAddressNoTimeSerializer);

         if (protocolVersion < KnownVersion.V106)
         {
            return;
         }

         output.WriteWithSerializer(message.SenderAddress, protocolVersion, this.networkAddressNoTimeSerializer);
         output.WriteULong(message.Nonce);
         output.WriteVarString(message.UserAgent);
         output.WriteInt(message.StartHeight);

         if (protocolVersion < KnownVersion.V70001)
         {
            return;
         }

         output.WriteBool(message.Relay);
      }

      public override VersionMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         var message = new VersionMessage
         {
            Version = reader.ReadInt(),
            Services = reader.ReadULong(),
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.ReadLong()),
            ReceiverAddress = reader.ReadWithSerializer(protocolVersion, this.networkAddressNoTimeSerializer)
         };

         if (message.Version < KnownVersion.V106)
         {
            return message;
         }

         message.SenderAddress = reader.ReadWithSerializer(protocolVersion, this.networkAddressNoTimeSerializer);
         message.Nonce = reader.ReadULong();
         message.UserAgent = reader.ReadVarString();
         message.StartHeight = reader.ReadInt();

         if (message.Version < KnownVersion.V70001)
         {
            return message;
         }

         message.Relay = reader.ReadBool();
         return message;
      }
   }
}

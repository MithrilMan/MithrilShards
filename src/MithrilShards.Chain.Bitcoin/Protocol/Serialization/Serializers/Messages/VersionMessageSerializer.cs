using System;
using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class VersionMessageSerializer : BitcoinNetworkMessageSerializerBase<VersionMessage>
   {
      private readonly IProtocolTypeSerializer<NetworkAddressNoTime> _networkAddressNoTimeSerializer;

      public VersionMessageSerializer(IProtocolTypeSerializer<NetworkAddressNoTime> networkAddressNoTimeSerializer)
      {
         this._networkAddressNoTimeSerializer = networkAddressNoTimeSerializer;
      }

      public override void Serialize(VersionMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
      {
         // version message doesn't have to look into passed protocolVersion but rely on it's message.Version.
         protocolVersion = message.Version;

         output.WriteInt(message.Version);
         output.WriteULong(message.Services);
         output.WriteLong(message.Timestamp.ToUnixTimeSeconds());
         output.WriteWithSerializer(message.ReceiverAddress!, protocolVersion, this._networkAddressNoTimeSerializer);

         if (protocolVersion < KnownVersion.V106)
         {
            return;
         }

         output.WriteWithSerializer(message.SenderAddress!, protocolVersion, this._networkAddressNoTimeSerializer);
         output.WriteULong(message.Nonce);
         output.WriteVarString(message.UserAgent!);
         output.WriteInt(message.StartHeight);

         if (protocolVersion < KnownVersion.V70001)
         {
            return;
         }

         output.WriteBool(message.Relay);
      }

      public override VersionMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
      {
         var message = new VersionMessage
         {
            Version = reader.ReadInt(),
            Services = reader.ReadULong(),
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(reader.ReadLong()),
            ReceiverAddress = reader.ReadWithSerializer(protocolVersion, this._networkAddressNoTimeSerializer)
         };

         if (message.Version < KnownVersion.V106)
         {
            return message;
         }

         message.SenderAddress = reader.ReadWithSerializer(protocolVersion, this._networkAddressNoTimeSerializer);
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

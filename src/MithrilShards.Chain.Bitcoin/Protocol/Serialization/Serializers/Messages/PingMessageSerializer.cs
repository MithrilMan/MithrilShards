﻿using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class PingMessageSerializer : NetworkMessageSerializerBase<PingMessage>
   {
      public PingMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(PingMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         if (protocolVersion < KnownVersion.V60001)
         {
            return;
         }

         output.WriteULong(message.Nonce);
      }

      public override PingMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         var message = new PingMessage();

         if (protocolVersion < KnownVersion.V60001)
         {
            return message;
         }

         message.Nonce = reader.ReadULong();

         return message;
      }
   }
}
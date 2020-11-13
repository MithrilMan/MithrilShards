using System.Buffers;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Example.Network;
using MithrilShards.Example.Protocol.Messages;

namespace MithrilShards.Example.Protocol.Serialization.Serializers.Messages
{
   /// <summary>
   /// PingMessage serializer, used to serialize and send through the network a <see cref="PingMessage"/>
   /// </summary>
   /// <seealso cref="ExampleNetworkMessageSerializerBase{PingMessage}" />
   public class PingMessageSerializer : ExampleNetworkMessageSerializerBase<PingMessage>
   {
      public PingMessageSerializer(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(PingMessage message, int protocolVersion, ExamplePeerContext peerContext, IBufferWriter<byte> output)
      {
         // suppose we created the nonce parameter in version 2, we wouldn't serialize this field if we are talking with a V2 peer
         if (protocolVersion < KnownVersion.V2)
         {
            return;
         }

         output.WriteULong(message.Nonce);
      }

      public override PingMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ExamplePeerContext peerContext)
      {
         var message = new PingMessage();

         // suppose we created the nonce parameter in version 2, we can't expect to receive this value from a peer on version prior to V2 peer payload
         if (protocolVersion < KnownVersion.V2)
         {
            return message;
         }

         message.Nonce = reader.ReadULong();

         return message;
      }
   }
}
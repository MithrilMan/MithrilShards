using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers
{
   public class PingMessageSerializer : NetworkMessageSerializerBase<PingMessage>
   {
      public PingMessageSerializer(IChainDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(PingMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         output.WriteULong(message.Nonce);
      }

      public override PingMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         var message = new PingMessage
         {
            Nonce = reader.ReadULong()
         };

         return message;
      }
   }
}
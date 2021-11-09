using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Example.Network;
using MithrilShards.Example.Protocol.Messages;
using MithrilShards.Example.Protocol.Types;

namespace MithrilShards.Example.Protocol.Serialization.Serializers.Messages;

/// <summary>
/// PongMessage serializer, used to serialize and send through the network a <see cref="PongMessage"/>
/// </summary>
/// <seealso cref="ExampleNetworkMessageSerializerBase{PongMessage}" />
public class PongMessageSerializer : ExampleNetworkMessageSerializerBase<PongMessage>
{
   readonly IProtocolTypeSerializer<PongFancyResponse> _pongFancyResponseSerializator;

   public PongMessageSerializer(IProtocolTypeSerializer<PongFancyResponse> pongFancyResponseSerializator)
   {
      /// since the pong message has a complex type that can be reused in other payload (well, this is specific to pong but you get the idea) we are implementing a custom
      /// type serializer and inject it into this message serializer
      _pongFancyResponseSerializator = pongFancyResponseSerializator;
   }

   public override void Serialize(PongMessage message, int protocolVersion, ExamplePeerContext peerContext, IBufferWriter<byte> output)
   {
      output.WriteWithSerializer(message.PongFancyResponse!, protocolVersion, _pongFancyResponseSerializator);
   }

   public override PongMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ExamplePeerContext peerContext)
   {
      return new PongMessage
      {
         PongFancyResponse = reader.ReadWithSerializer(protocolVersion, _pongFancyResponseSerializator)
      };
   }
}

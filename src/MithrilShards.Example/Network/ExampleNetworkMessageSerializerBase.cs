using System.Buffers;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Example.Network
{
   /// <summary>
   /// The only reason to have this message serializer instead of using directly the <see cref="NetworkMessageSerializerBase"/> is because we want to expose our <see cref="IPeerContext"/>
   /// as a ExamplePeerContext
   /// </summary>
   /// <typeparam name="TMessageType">The type of the message type.</typeparam>
   /// <seealso cref="MithrilShards.Core.Network.Protocol.Serialization.NetworkMessageSerializerBase{TMessageType}" />
   public abstract class ExampleNetworkMessageSerializerBase<TMessageType> : NetworkMessageSerializerBase<TMessageType> where TMessageType : INetworkMessage, new()
   {
      public ExampleNetworkMessageSerializerBase(INetworkDefinition chainDefinition) : base(chainDefinition) { }

      public override TMessageType Deserialize(ref SequenceReader<byte> reader, int protocolVersion, IPeerContext peerContext)
         => this.Deserialize(ref reader, protocolVersion, (ExamplePeerContext)peerContext);

      public override void Serialize(TMessageType message, int protocolVersion, IPeerContext peerContext, IBufferWriter<byte> output)
         => Serialize(message, protocolVersion, (ExamplePeerContext)peerContext, output);

      public abstract TMessageType Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ExamplePeerContext peerContext);

      public abstract void Serialize(TMessageType message, int protocolVersion, ExamplePeerContext peerContext, IBufferWriter<byte> output);
   }
}
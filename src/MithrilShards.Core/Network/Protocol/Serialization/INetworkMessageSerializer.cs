using System;
using System.Buffers;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   public interface INetworkMessageSerializer
   {
      /// <summary>
      /// Gets the type of the message managed by the serializer.
      /// </summary>
      /// <returns></returns>
      Type GetMessageType();


      /// <summary>
      /// Serializes the specified message writing it into <paramref name="output"/>.
      /// </summary>
      /// <param name="message">The message to serialize.</param>
      /// <param name="protocolVersion">The protocol version to use to serialize the message.</param>
      /// <param name="peerContext"></param>
      /// <param name="output">The output buffer used to store data into.</param>
      void Serialize(INetworkMessage message, int protocolVersion, IPeerContext peerContext, IBufferWriter<byte> output);

      INetworkMessage Deserialize(ref ReadOnlySequence<byte> data, int protocolVersion, IPeerContext peerContext);
   }
}

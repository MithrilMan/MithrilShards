using System.Buffers;

namespace MithrilShards.Core.Network.Protocol.Serialization;

public interface IProtocolTypeSerializer<TProtocolType>
{
   /// <summary>
   /// Serializes the specified protocol data type writing it into <paramref name="writer" />.
   /// </summary>
   /// <param name="typeInstance">The type to serialize.</param>
   /// <param name="protocolVersion">The protocol version to use to serialize the message.</param>
   /// <param name="writer">The writer.</param>
   /// <param name="options">The options.</param>
   /// <returns>
   /// number of written bytes
   /// </returns>
   int Serialize(TProtocolType typeInstance, int protocolVersion, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null);

   /// <summary>
   /// Deserializes the specified message reading it from the <paramref name="reader" />.
   /// </summary>
   /// <param name="reader">The reader.</param>
   /// <param name="protocolVersion">The protocol version to use to deserialize the data.</param>
   /// <param name="options"></param>
   /// <returns>number of read bytes</returns>
   TProtocolType Deserialize(ref SequenceReader<byte> reader, int protocolVersion, ProtocolTypeSerializerOptions? options = null);
}

using System.Buffers;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   public interface ISerializableProtocolType
   {
      int Serialize(IBufferWriter<byte> writer, int protocolVersion);

      void Deserialize(ref SequenceReader<byte> reader, int protocolVersion);
   }

   public interface ISerializableProtocolTypeEndiannessAware : ISerializableProtocolType
   {
      int Serialize(IBufferWriter<byte> writer, int protocolVersion, bool isLittleEndian);

      void Deserialize(ref SequenceReader<byte> reader, int protocolVersion, bool isLittleEndian);
   }
}

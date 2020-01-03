using System.Buffers;

namespace MithrilShards.Core.Network.Protocol.Serialization {
   public interface ISerializableProtocolType {
      int Serialize(IBufferWriter<byte> writer);

      void Deserialize(ref SequenceReader<byte> reader);
   }

   public interface ISerializableProtocolTypeEndiannessAware : ISerializableProtocolType {
      int Serialize(IBufferWriter<byte> writer, bool isLittleEndian);

      void Deserialize(ref SequenceReader<byte> reader, bool isLittleEndian);
   }
}

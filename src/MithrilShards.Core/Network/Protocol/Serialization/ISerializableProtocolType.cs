using System.Buffers;

namespace MithrilShards.Core.Network.Protocol.Serialization {
   public interface ISerializableProtocolType {
      /// <summary>
      /// Gets the internal name of the serializable type.
      /// E.g. for a bitcoin-like type an internal name may be "int32_t" in order to have an easier way to identify types
      /// </summary>
      /// <value>
      /// The name of the internal.
      /// </value>
      string InternalName { get; }

      /// <summary>
      /// Gets the length of the type, if known, otherwise -1.
      /// </summary>
      int Length { get; }
   }

   public interface ISerializableProtocolType<TProtocolType> : ISerializableProtocolType where TProtocolType : ISerializableProtocolType<TProtocolType> {
      int Serialize(IBufferWriter<byte> writer);

      void Deserialize(SequenceReader<byte> data);
   }

   public interface ISerializableProtocolTypeEndiannessAware<TProtocolType> : ISerializableProtocolType where TProtocolType : ISerializableProtocolTypeEndiannessAware<TProtocolType> {
      int Serialize(IBufferWriter<byte> writer, bool isLittleEndian);

      void Deserialize(SequenceReader<byte> data, bool isLittleEndian);
   }
}

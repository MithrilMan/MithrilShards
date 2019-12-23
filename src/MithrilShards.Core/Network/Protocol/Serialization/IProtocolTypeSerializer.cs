namespace MithrilShards.Core.Network.Protocol.Serialization {
   public interface IProtocolTypeSerializer<TSerializableProtocolType> where TSerializableProtocolType : ISerializableProtocolType {
      /// <summary>
      /// Serializes the specified value in a byte array.
      /// </summary>
      /// <param name="value">The value to serialize.</param>
      /// <returns></returns>
      byte[] Serialize(TSerializableProtocolType value);

      /// <summary>
      /// Deserializes the specified raw byte array value into a <see cref="TSerializableProtocolType"/> value.
      /// </summary>
      /// <param name="rawValue">The raw value to Deserialize.</param>
      /// <returns></returns>
      TSerializableProtocolType Deserialize(byte[] rawValue);
   }
}

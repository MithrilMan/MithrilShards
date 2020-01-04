using System;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   /// <summary>
   /// Define the requirements and the serialization type in order to serialize and deserialize current message in the appropriate context.
   /// </summary>
   [AttributeUsage(AttributeTargets.Property)]
   public class NetworkMessageFieldAttribute : Attribute
   {

      public Type SerializedType { get; }

      /// <summary>
      /// Gets ordinal position of the information in the payload.
      /// </summary>
      public int Position { get; }

      /// <summary>
      /// The minimum protocol version in which the marked field is valid
      /// </summary>
      public int MinVersion { get; }

      /// <summary>
      /// The maximum version in which the marked field is valid
      /// </summary>
      public int MaxVersion { get; }

      public NetworkMessageFieldAttribute(Type serializedType, int position, int minVersion = int.MinValue, int maxVersion = int.MaxValue)
      {
         this.SerializedType = serializedType;
         this.Position = position;
         this.MinVersion = minVersion;
         this.MaxVersion = maxVersion;

         if (!serializedType.IsAssignableFrom(typeof(ISerializableProtocolType)))
         {
            throw new ArgumentException($"Invalid {nameof(this.SerializedType)}. It must be a Type that implements {nameof(ISerializableProtocolType)}", nameof(serializedType));
         }
      }
   }
}
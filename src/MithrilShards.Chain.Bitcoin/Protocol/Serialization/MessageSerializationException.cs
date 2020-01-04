using System;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization
{
   [Serializable]
   public class MessageSerializationException : Exception
   {
      public MessageSerializationException() { }
      public MessageSerializationException(string message) : base(message) { }
      public MessageSerializationException(string message, Exception inner) : base(message, inner) { }
      protected MessageSerializationException(
       System.Runtime.Serialization.SerializationInfo info,
       System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
   }
}

using System;

namespace MithrilShards.Core.Network.Protocol {

   [Serializable]
   public class ProtocolViolationException : Exception {
      public ProtocolViolationException() { }
      public ProtocolViolationException(string message) : base(message) { }
      public ProtocolViolationException(string message, Exception inner) : base(message, inner) { }
      protected ProtocolViolationException(
       System.Runtime.Serialization.SerializationInfo info,
       System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
   }
}

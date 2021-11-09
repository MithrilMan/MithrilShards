using System;

namespace MithrilShards.Core.Crypto;

[Serializable]
public class HashGeneratorException : Exception
{
   public HashGeneratorException() { }
   public HashGeneratorException(string message) : base(message) { }
   public HashGeneratorException(string message, Exception inner) : base(message, inner) { }
   protected HashGeneratorException(
    System.Runtime.Serialization.SerializationInfo info,
    System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

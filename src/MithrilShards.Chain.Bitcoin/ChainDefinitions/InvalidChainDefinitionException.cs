﻿using System;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{

   [Serializable]
   public class InvalidChainDefinitionException : Exception
   {
      public InvalidChainDefinitionException() { }
      public InvalidChainDefinitionException(string message) : base(message) { }
      public InvalidChainDefinitionException(string message, Exception inner) : base(message, inner) { }
      protected InvalidChainDefinitionException(
       System.Runtime.Serialization.SerializationInfo info,
       System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
   }
}

﻿using System;

namespace MithrilShards.Core.Forge
{
   /// <summary>
   /// Exception thrown by <see cref="ForgeBuilder.Build"/>.
   /// </summary>
   /// <seealso cref="ForgeBuilder.Build"/>
   [Serializable]
   public class ForgeBuilderException : Exception
   {
      public ForgeBuilderException() { }
      public ForgeBuilderException(string message) : base(message) { }
      public ForgeBuilderException(string message, Exception inner) : base(message, inner) { }
      protected ForgeBuilderException(
       System.Runtime.Serialization.SerializationInfo info,
       System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
   }
}

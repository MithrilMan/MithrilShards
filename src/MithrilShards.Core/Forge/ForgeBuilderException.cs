using System;

namespace MithrilShards.Core.Forge {
   /// <summary>
   /// Exception thrown by <see cref="ForgeBuilder.Build"/>.
   /// </summary>
   /// <seealso cref="ForgeBuilder.Build"/>
   public class ForgeBuilderException : Exception {
      public ForgeBuilderException(string message) : base(message) {
      }
   }
}

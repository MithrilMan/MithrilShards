using System;

namespace MithrilShards.Logging.ConsoleTableFormatter
{

   [Serializable]
   public class TableAlreadyDefinedException : Exception
   {
      private const string DEFAULT_ERROR_MESSAGE = "Table already prepared, cannot alter the definition.";
      public TableAlreadyDefinedException() : base(DEFAULT_ERROR_MESSAGE) { }
      public TableAlreadyDefinedException(string message) : base(message) { }
      public TableAlreadyDefinedException(string message, Exception inner) : base(message, inner) { }
      protected TableAlreadyDefinedException(
       System.Runtime.Serialization.SerializationInfo info,
       System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
   }
}

using System;

namespace MithrilShards.Logging.TableFormatter;

[Serializable]
public class TableAlreadyDefinedException : Exception
{
   private const string DEFAULT_ERROR_MESSAGE = "Table already prepared, cannot alter the definition.";
   public TableAlreadyDefinedException() : base(DEFAULT_ERROR_MESSAGE) { }
   public TableAlreadyDefinedException(string message) : base(message) { }
   public TableAlreadyDefinedException(string message, Exception inner) : base(message, inner) { }
}

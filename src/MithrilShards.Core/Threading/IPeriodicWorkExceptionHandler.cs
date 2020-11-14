using System;

namespace MithrilShards.Core.Threading
{
   public interface IPeriodicWorkExceptionHandler
   {
      void OnPeriodicWorkException(IPeriodicWork failedWork, Exception ex, ref Feedback feedback);

      public class Feedback
      {
         /// <summary>
         /// Gets or sets a value indicating whether the periodic work has to continue its execution.
         /// </summary>
         public bool ContinueExecution { get; set; }

         /// <summary>
         /// Gets or sets a value indicating whether the periodic work exception is critical.
         /// </summary>
         public bool IsCritical { get; set; }

         /// <summary>
         /// Gets or sets the message explaining any problems that the periodic work exception can cause.
         /// </summary>
         public string? Message { get; set; }

         internal Feedback(bool continueExecution, bool isCritical, string? message)
         {
            this.ContinueExecution = continueExecution;
            this.IsCritical = isCritical;
            this.Message = message;
         }
      }
   }
}

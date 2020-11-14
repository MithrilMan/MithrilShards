using System.Collections.Generic;

namespace MithrilShards.Example
{
   public interface IQuoteService
   {
      List<string> Quotes { get; }

      string GetRandomQuote();
   }
}
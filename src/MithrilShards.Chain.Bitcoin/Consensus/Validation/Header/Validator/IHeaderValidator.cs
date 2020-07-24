using System.Threading.Tasks;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public interface IHeaderValidator
   {
      /// <summary>
      /// Requests the header validation process on issued headers.
      /// </summary>
      /// <param name="headers">The headers to validate</param>
      /// <returns></returns>
      ValueTask RequestValidationAsync(HeadersToValidate headers);
   }
}
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;

public interface IHeaderValidator : IHostedService
{
   /// <summary>
   /// Requests the header validation process on issued headers.
   /// </summary>
   /// <param name="headers">The headers to validate</param>
   /// <returns></returns>
   ValueTask RequestValidationAsync(HeadersToValidate headers);
}

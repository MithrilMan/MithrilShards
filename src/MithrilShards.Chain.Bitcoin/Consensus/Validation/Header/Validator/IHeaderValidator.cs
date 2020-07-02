using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public interface IHeaderValidator : IHostedService
   {
      Task RequestValidationAsync(HeadersToValidate header);
   }
}
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Validator
{
   public interface IBlockValidator : IHostedService
   {
      /// <summary>
      /// Requests the block validation process on issued block.
      /// </summary>
      /// <param name="block">The block.</param>
      /// <returns></returns>
      ValueTask RequestValidationAsync(BlockToValidate block);
   }
}

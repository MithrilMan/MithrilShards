using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Rules
{
   public class CheckMerkleRoot : IBlockValidationRule
   {
      readonly ILogger<CheckMerkleRoot> logger;

      public CheckMerkleRoot(ILogger<CheckMerkleRoot> logger)
      {
         this.logger = logger;
      }


      public bool Check(IBlockValidationContext context, ref BlockValidationState validationState)
      {


         return true;
      }
   }
}
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules
{
   public class CheckProofOfWork : IHeaderValidationRule
   {
      readonly ILogger<CheckProofOfWork> logger;
      readonly IProofOfWorkCalculator proofOfWorkCalculator;

      public CheckProofOfWork(ILogger<CheckProofOfWork> logger, IProofOfWorkCalculator proofOfWorkCalculator)
      {
         this.logger = logger;
         this.proofOfWorkCalculator = proofOfWorkCalculator;
      }

      public bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
      {
         BlockHeader header = context.Header;

         // Check proof of work matches claimed amount
         //if (fCheckPOW && !CheckProofOfWork(block.GetHash(), block.nBits, consensusParams))
         if (!this.proofOfWorkCalculator.CheckProofOfWork(header))
         {
            validationState.Invalid(BlockValidationFailureContext.BlockInvalidHeader, "high-hash", "proof of work failed");
            return false;
         }

         return true;
      }
   }
}
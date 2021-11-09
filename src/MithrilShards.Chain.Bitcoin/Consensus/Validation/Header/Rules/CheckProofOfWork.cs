using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules;

public class CheckProofOfWork : IHeaderValidationRule
{
   readonly ILogger<CheckProofOfWork> _logger;
   readonly IProofOfWorkCalculator _proofOfWorkCalculator;

   public CheckProofOfWork(ILogger<CheckProofOfWork> logger, IProofOfWorkCalculator proofOfWorkCalculator)
   {
      _logger = logger;
      _proofOfWorkCalculator = proofOfWorkCalculator;
   }

   public bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
   {
      BlockHeader header = context.Header;

      // Check proof of work matches claimed amount
      //if (fCheckPOW && !CheckProofOfWork(block.GetHash(), block.nBits, consensusParams))
      if (!_proofOfWorkCalculator.CheckProofOfWork(header))
      {
         validationState.Invalid(BlockValidationStateResults.InvalidHeader, "high-hash", "proof of work failed");
         return false;
      }

      return true;
   }
}

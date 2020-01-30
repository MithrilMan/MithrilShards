using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IConsensusValidator
   {
      bool ValidateHeader(BlockHeader header, out BlockValidationState validationState);
   }
}
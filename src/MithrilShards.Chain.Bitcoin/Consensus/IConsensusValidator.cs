using System.Diagnostics.CodeAnalysis;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IConsensusValidator
   {
      bool ProcessNewBlockHeaders(BlockHeader[] headers, out BlockValidationState state, [MaybeNullWhen(false)]out HeaderNode lastProcessedHeader);

      void CheckBlockIndex();
   }
}
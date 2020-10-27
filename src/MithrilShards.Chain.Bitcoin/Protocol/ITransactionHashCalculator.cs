using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   public interface ITransactionHashCalculator
   {
      UInt256 ComputeHash(Transaction transaction, int protocolVersion);

      UInt256 ComputeWitnessHash(Transaction transaction, int protocolVersion);
   }
}
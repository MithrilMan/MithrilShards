using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol;

public interface IBlockHeaderHashCalculator
{
   UInt256 ComputeHash(BlockHeader header, int protocolVersion);
}

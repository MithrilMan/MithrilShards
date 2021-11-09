using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus;

public interface IHeaderMedianTimeCalculator
{
   /// <summary>
   /// Calculate (backward) the median block time over a median TimeSpan window from this entry in the chain.
   /// </summary>
   /// <param name="startingBlockHash">The starting block hash.</param>
   /// <param name="startingBlockHeight">Height of the starting block.</param>
   /// <returns>
   /// The median block time.
   /// </returns>
   uint Calculate(UInt256 startingBlockHash, int startingBlockHeight);
}

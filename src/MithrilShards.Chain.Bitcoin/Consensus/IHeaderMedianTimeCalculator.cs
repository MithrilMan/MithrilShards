using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IHeaderMedianTimeCalculator
   {
      /// <summary>
      /// Calculate (backward) the median block time over <see cref="medianTimeSpan" /> window from this entry in the chain.
      /// </summary>
      /// <param name="startingBlock">The starting block.</param>
      /// <param name="startingBlockHeight">Height of the starting block.</param>
      /// <returns>
      /// The median block time.
      /// </returns>
      uint Calculate(UInt256 startingBlockHash, int startingBlockHeight);
   }
}
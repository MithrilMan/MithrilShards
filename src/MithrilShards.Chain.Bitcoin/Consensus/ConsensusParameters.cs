using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public class ConsensusParameters : IConsensusParameters
   {
      public UInt256 Genesis { get; set; } = null!;

      public int SegwitHeight { get; set; }

      public int SubsidyHalvingInterval { get; set; }

      public Target PowLimit { get; set; } = null!;

      public long PowTargetTimespan { get; set; }

      public long PowTargetSpacing { get; set; }

      public bool PowAllowMinDifficultyBlocks { get; set; }

      public bool PowNoRetargeting { get; set; }

      public UInt256 MinimumChainWork { get; set; } = null!;
   }
}

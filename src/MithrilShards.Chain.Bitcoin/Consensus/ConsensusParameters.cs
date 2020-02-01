using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public class ConsensusParameters : IConsensusParameters
   {
      public UInt256 Genesis { get; set; } = null!;

      public long PowTargetSpacing { get; set; }

      public int SegwitHeight { get; set; }

      public int SubsidyHalvingInterval { get; set; }

      public UInt256 PowLimit { get; set; } = null!;

      public long PowTargetTimespan { get; set; }

      public UInt256 MinimumChainWork { get; set; } = null!;
   }
}

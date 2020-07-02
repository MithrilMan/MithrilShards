using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public class ConsensusParameters : IConsensusParameters
   {
      public BlockHeader GenesisHeader { get; set; } = null!;

      public int SegwitHeight { get; set; }

      public int SubsidyHalvingInterval { get; set; }

      public Target PowLimit { get; set; } = null!;

      public uint PowTargetTimespan { get; set; }

      public uint PowTargetSpacing { get; set; }

      public bool PowAllowMinDifficultyBlocks { get; set; }

      public bool PowNoRetargeting { get; set; }

      public Target MinimumChainWork { get; set; } = null!;

   }
}

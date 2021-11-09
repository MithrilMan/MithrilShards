using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus;

/// <summary>
/// TODO: split parameters in sections, allowing to set them in bounch of calls like
/// SetPowOptions(), SetMonetaryOptions(), etc...
/// </summary>
/// <seealso cref="MithrilShards.Chain.Bitcoin.Consensus.IConsensusParameters" />
public class ConsensusParameters : IConsensusParameters
{
   public BlockHeader GenesisHeader { get; }

   public int SubsidyHalvingInterval { get; }

   public Target PowLimit { get; }

   public uint PowTargetTimespan { get; }

   public uint PowTargetSpacing { get; }

   public bool PowAllowMinDifficultyBlocks { get; }

   public bool PowNoRetargeting { get; }

   public Target MinimumChainWork { get; }

   public uint MaxBlockWeight { get; }

   public int WitnessScaleFactor { get; }

   public int SegwitHeight { get; }

   public long MaxMoney { get; }

   public ConsensusParameters(BlockHeader genesisHeader,
                              int subsidyHalvingInterval,
                              Target powLimit,
                              uint powTargetTimespan,
                              uint powTargetSpacing,
                              bool powAllowMinDifficultyBlocks,
                              bool powNoRetargeting,
                              Target minimumChainWork,
                              uint maxBlockSerializedSize,
                              int witnessScaleFactor,
                              int segwitHeight,
                              long maxMoney
      )
   {
      GenesisHeader = genesisHeader;
      SubsidyHalvingInterval = subsidyHalvingInterval;
      PowLimit = powLimit;
      PowTargetTimespan = powTargetTimespan;
      PowTargetSpacing = powTargetSpacing;
      PowAllowMinDifficultyBlocks = powAllowMinDifficultyBlocks;
      PowNoRetargeting = powNoRetargeting;
      MinimumChainWork = minimumChainWork;
      MaxBlockWeight = maxBlockSerializedSize;
      WitnessScaleFactor = witnessScaleFactor;
      SegwitHeight = segwitHeight;
      MaxMoney = maxMoney;
   }
}

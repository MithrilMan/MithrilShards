﻿using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IConsensusParameters
   {
      /// <summary>
      /// Gets the genesis header.
      /// </summary>
      BlockHeader GenesisHeader { get; }

      /// <summary>
      /// Gets the maximum allowed PoW.
      /// </summary>
      /// <value>
      /// The pow limit.
      /// </value>
      Target PowLimit { get; }

      /// <summary>
      /// Gets the pow target timespan, in seconds.
      /// </summary>
      /// <value>
      /// The pow target timespan.
      /// </value>
      uint PowTargetTimespan { get; }

      /// <summary>
      /// Gets or sets the pow target spacing, in seconds.
      /// </summary>
      /// <value>
      /// The pow target spacing.
      /// </value>
      uint PowTargetSpacing { get; }

      /// <summary>
      /// Gets a value indicating whether a block can be mined with the minimum difficulty, to use just on test networks.
      /// </summary>
      /// <remarks>
      /// Set this to <see langword="false"/> for any serious network, use <see langword="true"/> only for test purposes or test networks.
      /// </remarks>
      bool PowAllowMinDifficultyBlocks { get; }

      /// <summary>
      /// Gets a value indicating whether PoW retargeting is disabled or not.
      /// </summary>
      /// <value>
      ///   <c>true</c> if PoW retargeting is disabled; otherwise, <c>false</c>.
      /// </value>
      bool PowNoRetargeting { get; }

      /// <summary>
      /// Gets the subsidy halving interval, in blocks.
      /// </summary>
      /// <value>
      /// The subsidy halving interval,, in blocks.
      /// </value>
      int SubsidyHalvingInterval { get; }

      /// <summary>
      /// Height of segwit activation.
      /// </summary>
      /// <value>
      /// The height of the segwit.
      /// </value>
      int SegwitHeight { get; }


      /// <summary>
      /// Gets the minimum chain work the best chain should have.
      /// Can be overridden by settings
      /// </summary>
      /// <value>
      /// The minimum chain work.
      /// </value>
      Target MinimumChainWork { get; }

      /// <summary>The maximum allowed size for a serialized block, in bytes (only for buffer size limits). </summary>
      uint MaxBlockWeight { get; }

      /// <summary>Scale of witness vs other transaction data. e.g. if set to 4,
      /// then witnesses have 1/4 the weight per byte of other transaction data.
      /// </summary>
      int WitnessScaleFactor { get; }

      /// <summary>
      /// The maximum amount of coins in any transaction.
      /// </summary>
      long MaxMoney { get; }
   }
}

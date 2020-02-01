using System;
using System.Collections.Generic;
using System.Text;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IConsensusParameters
   {
      /// <summary>
      /// Gets the hash of the first block (aka genesis) of the chain.
      /// </summary>
      UInt256 Genesis { get; }

      /// <summary>
      /// Gets or sets the pow target spacing.
      /// </summary>
      /// <value>
      /// The pow target spacing.
      /// </value>
      long PowTargetSpacing { get; }

      /// <summary>
      /// Gets the maximum allowed PoW.
      /// </summary>
      /// <value>
      /// The pow limit.
      /// </value>
      UInt256 PowLimit { get; }

      /// <summary>
      /// Gets the pow target timespan.
      /// </summary>
      /// <value>
      /// The pow target timespan.
      /// </value>
      long PowTargetTimespan { get; }

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
      UInt256 MinimumChainWork { get; }
   }
}

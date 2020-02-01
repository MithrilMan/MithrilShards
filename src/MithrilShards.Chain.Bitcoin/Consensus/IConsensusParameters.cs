using System;
using System.Collections.Generic;
using System.Text;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IConsensusParameters
   {
      /// <summary>
      /// Gets or sets the pow target spacing.
      /// </summary>
      /// <value>
      /// The pow target spacing.
      /// </value>
      long PowTargetSpacing { get; }

      /// <summary>
      /// Gets the hash of the first block (aka genesis) of the chain.
      /// </summary>
      UInt256 Genesis { get; }
   }
}

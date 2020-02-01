using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public class ConsensusParameters : IConsensusParameters
   {
      /// <summary>
      /// Gets or sets the pow target spacing.
      /// </summary>
      /// <value>
      /// The pow target spacing.
      /// </value>
      public long PowTargetSpacing { get; set; }

      /// <summary>
      /// Gets the hash of the first block (aka genesis) of the chain.
      /// </summary>
      public UInt256 Genesis { get; set; } = null!;
   }
}

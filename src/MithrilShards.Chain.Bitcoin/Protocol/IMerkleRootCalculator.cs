using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   public interface IMerkleRootCalculator
   {
      /// <summary>
      /// Computes the Merkle root.
      /// </summary>
      /// <param name="hashes">The hashes to be used to compute the merklee root.</param>
      /// <returns></returns>
      UInt256 ComputeMerkleRoot(IList<UInt256> hashes);

      /// <summary>
      /// Gets the block Merkle root.
      /// </summary>
      /// <param name="block">The block for which to compute Merkle root.</param>
      /// <returns></returns>
      UInt256 GetBlockMerkleRoot(Block block);

      /// <summary>
      /// Gets the block witness Merkle root.
      /// </summary>
      /// <param name="block">The block for which to compute Merkle root.</param>
      /// <returns></returns>
      UInt256 GetBlockWitnessMerkleRoot(Block block);
   }
}

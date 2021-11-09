using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Rules;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Crypto;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   /// <summary>
   /// Merkle root calculator, implementing bitcoin flawed logic.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.IMerkleRootCalculator" />
   /// <remarks>
   /// Here a copy and past of original bitcoin comment on the flawled implementation:
   /// WARNING! If you're reading this because you're learning about crypto
   /// and/or designing a new system that will use merkle trees, keep in mind
   /// that the following merkle tree algorithm has a serious flaw related to
   /// duplicate txids, resulting in a vulnerability(CVE-2012-2459).
   ///
   /// The reason is that if the number of hashes in the list at a given level
   /// is odd, the last one is duplicated before computing the next level(which
   /// is unusual in Merkle trees). This results in certain sequences of
   /// transactions leading to the same merkle root.For example, these two
   /// trees:
   ///
   ///            A                 A
   ///          /   \             /   \
   ///         B     C          B       C
   ///        / \    |         / \     / \
   ///       D   E   F        D   E   F   F
   ///      / \ / \ / \      / \ / \ / \ / \
   ///      1 2 3 4 5 6      1 2 3 4 5 6 5 6
   ///
   /// for transaction lists [1,2,3,4,5,6] and[1, 2, 3, 4, 5, 6, 5, 6] (where 5 and
   /// 6 are repeated) result in the same root hash A(because the hash of both
   /// of (F) and (F, F) is C).
   ///
   /// The vulnerability results from being able to send a block with such a
   /// transaction list, with the same merkle root, and the same block hash as
   /// the original without duplication, resulting in failed validation.If the
   /// receiving node proceeds to mark that block as permanently invalid
   /// however, it will fail to accept further unmodified (and thus potentially
   /// valid) versions of the same block.We defend against this by detecting
   ///
   /// the case where we would hash two identical hashes at the end of the list
   /// together, and treating that identically to the block having an invalid
   ///
   /// merkle root. Assuming no double-SHA256 collisions, this will detect all
   /// known ways of changing the transactions without affecting the merkle
   /// root.
   /// ---
   ///
   /// This implementation doesn't check the attempt of using duplicate transactions, that check
   /// is done by <see cref="CheckMerkleRoot"/> validation rule.
   /// </remarks>
   public class BitcoinFlawedMerkleRootCalculator : IMerkleRootCalculator
   {
      readonly ILogger<BitcoinFlawedMerkleRootCalculator> _logger;

      public BitcoinFlawedMerkleRootCalculator(ILogger<BitcoinFlawedMerkleRootCalculator> logger)
      {
         _logger = logger;
      }

      public UInt256 ComputeMerkleRoot(IList<UInt256> hashes)
      {
         if (hashes.Count == 0) return UInt256.Zero;
         if (hashes.Count == 1) return hashes[0];

         bool oddHashes = (hashes.Count & 1) == 1;
         //ensure to allocate one more item if hashes are odd.
         var hashesList = new List<UInt256>(oddHashes ? hashes.Count + 1 : hashes.Count);

         for (int i = 0; i < hashes.Count; i++)
         {
            hashesList.Add(hashes[i]);
         }

         // if odd, duplicate last element
         if (oddHashes)
         {
            hashesList.Add(hashes[hashes.Count - 1]);
         }

         Span<byte> pairOfHashes = stackalloc byte[64];
         int elementsCount = hashesList.Count;
         while (elementsCount > 1)
         {
            int newHashPosition = 0;
            for (int pos = 0; pos + 1 < elementsCount; pos += 2)
            {
               hashesList[pos].GetBytes().CopyTo(pairOfHashes);
               hashesList[pos + 1].GetBytes().CopyTo(pairOfHashes.Slice(32));

               hashesList[newHashPosition++] = HashGenerator.DoubleSha256AsUInt256(pairOfHashes);
            }

            if (newHashPosition > 1 && (newHashPosition & 1) == 1)
            {
               hashesList[newHashPosition] = hashesList[newHashPosition - 1];
               newHashPosition++;
            }

            hashesList.RemoveRange(newHashPosition, elementsCount - newHashPosition);
            elementsCount = newHashPosition;
         }

         return hashesList[0];
      }

      public UInt256 GetBlockMerkleRoot(Block block)
      {
         var leaves = new List<UInt256>(block.Transactions!.Length);
         foreach (Transaction tx in block.Transactions)
         {
            leaves.Add(tx.Hash!);
         }

         return ComputeMerkleRoot(leaves);
      }

      public UInt256 GetBlockWitnessMerkleRoot(Block block)
      {
         //var leaves = new List<UInt256>(block.Transactions!.Length);
         //leaves.Add(UInt256.Zero); // The witness hash of the coinbase is 0.

         //foreach (Transaction tx in block.Transactions)
         //{
         //   leaves.Add(tx.GetWitnessHash());
         //}

         //return ComputeMerkleRoot(leaves);
         throw new NotImplementedException();
      }
   }
}

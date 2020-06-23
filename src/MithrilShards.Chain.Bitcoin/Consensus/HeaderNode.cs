using System;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// Represents a node in a linked list (tree) of headers.
   /// It exposes current node height, parent and cumulative ChainWork
   /// </summary>
   public class HeaderNode
   {
      /// <summary>
      /// Gets the validity of the header.
      /// </summary>
      public HeaderValidityStatuses Validity { get; private set; }

      /// <summary>
      /// Gets the height this node takes in the tree representation of the whole hierarchy of headers.
      /// </summary>
      public int Height { get; }

      /// <summary>
      /// Gets the current header identifier.
      /// </summary>
      public UInt256 Hash { get; }

      /// <summary>
      /// Gets the previous header node.
      /// </summary>
      /// <value>
      /// The previous <see cref="HeaderNode"/>.
      /// </value>
      public HeaderNode? Previous { get; }

      /// <summary>
      /// Points to a previous item based on skip list implementation.
      /// </summary>
      /// <value>
      /// The previous <see cref="HeaderNode"/>.
      /// </value>
      public HeaderNode? Skip { get; }

      /// <summary>
      /// Total amount of work (expected number of hashes) in the chain up to and including this block.
      /// </summary>
      /// <remarks>It's an in-memory value only that get computed during the header tree building.</remarks>
      public Target ChainWork { get; internal set; }

      /// <summary>
      /// Initializes a new instance of the <see cref="HeaderNode"/> that references a genesis header.
      /// Only genesis header can have previous set to null.
      /// </summary>
      /// <param name="header">The header.</param>
      private HeaderNode(BlockHeader header)
      {
         if (header == null) ThrowHelper.ThrowArgumentNullException(nameof(header));
         if (header.Hash == null) ThrowHelper.ThrowArgumentException($"{nameof(header)} hash cannot be null.");

         this.Hash = header.Hash;
         this.Height = 0;
         this.Previous = null;
         this.ChainWork = Target.Zero;
         this.Skip = null;
         this.Validity = HeaderValidityStatuses.ValidMask;
      }

      internal HeaderNode(BlockHeader header, HeaderNode previous)
      {
         if (header == null) ThrowHelper.ThrowArgumentNullException(nameof(header));
         if (header.Hash == null) ThrowHelper.ThrowArgumentException($"{nameof(header)} hash cannot be null.");

         this.Hash = header.Hash;
         this.Height = previous.Height + 1;
         this.Previous = previous;
         this.ChainWork = previous.ChainWork + new Target(header.Bits).GetBlockProof();
         this.Skip = previous.GetAncestor(GetSkipHeight(this.Height));
         this.Validity = HeaderValidityStatuses.ValidTree;

         //pindexNew->nTimeMax = (pindexNew->pprev ? std::max(pindexNew->pprev->nTimeMax, pindexNew->nTime) : pindexNew->nTime);
      }

      /// <summary>
      /// Generates the genesis HeaderNode.
      /// </summary>
      /// <param name="genesisNode">The genesis node.</param>
      /// <returns></returns>
      public static HeaderNode GenerateGenesis(BlockHeader genesisNode)
      {
         return new HeaderNode(genesisNode);
      }

      private static int GetSkipHeight(int height)
      {
         if (height < 2)
         {
            return 0;
         }

         /// Turn the lowest '1' bit in the binary representation of a number into a '0'.
         int invertLowestOne(int n)
         {
            return n & (n - 1);
         }

         // Determine which height to jump back to. Any number strictly lower than height is acceptable,
         // but the following expression seems to perform well in simulations (max 110 steps to go back
         // up to 2**18 blocks).
         return ((height & 1) != 0) ? invertLowestOne(invertLowestOne(height - 1)) + 1 : invertLowestOne(height);
      }

      /// <summary>Calculates the amount of work that this block contributes to the total chain work.</summary>
      /// <returns>Amount of work.</returns>
      private static Target GetBlockProof(BlockHeader header)
      {
         //var target = new Target(header.Bits, out bool isNegative, out bool isOverflow);

         //if (isNegative || isOverflow || target == Target.Zero)
         //   return Target.Zero;

         //// We need to compute 2**256 / (bnTarget+1), but we can't represent 2**256
         //// as it's too large for an arith_uint256. However, as 2**256 is at least as large
         //// as bnTarget+1, it is equal to ((2**256 - bnTarget - 1) / (bnTarget+1)) + 1,
         //// or ~bnTarget / (bnTarget+1) + 1.
         //return (~target / (target + Target.FromRawValue(1))) + Target.FromRawValue(1);
         return new Target(header.Bits).GetBlockProof();
      }

      /// <summary>
      /// Check whether this block index entry is valid up to the passed validity level.
      /// </summary>
      /// <returns>
      ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
      /// </returns>
      public bool IsValid(HeaderValidityStatuses nUpTo = HeaderValidityStatuses.ValidTransactions)
      {
         // Only validity flags allowed.
         if ((nUpTo & HeaderValidityStatuses.ValidMask) != nUpTo)
         {
            ThrowHelper.ThrowArgumentException("Only validity flags are allowed");
         }

         if (this.Validity.HasFlag(HeaderValidityStatuses.FailedMask))
            return false;

         return (this.Validity & HeaderValidityStatuses.ValidMask) >= nUpTo;
      }

      public override string ToString()
      {
         return $"{this.Hash} ({this.Height})";
      }

      /// <summary>
      /// Finds the ancestor of this entry in the chain that matches the given block height.
      /// </summary>
      /// <param name="height">The block height to search for.</param>
      /// <returns>The ancestor of this chain at the specified height.</returns>
      public HeaderNode? GetAncestor(int height)
      {
         if (height > this.Height || height < 0)
            return null;

         HeaderNode current = this;
         int heightWalk = this.Height;
         while (heightWalk > height)
         {
            int heightSkip = GetSkipHeight(heightWalk);
            int heightSkipPrev = GetSkipHeight(heightWalk - 1); //walk.Previous.Skip.Height;
            if (current.Skip != null &&
               (heightSkip == height ||
                  (heightSkip > height && !(heightSkipPrev < (heightSkip - 2) &&
                                             heightSkipPrev >= height))))
            {
               // Only follow Skip if pprev->pskip isn't better than pskip->pprev.
               current = current.Skip;
               heightWalk = heightSkip;
            }
            else
            {
               current = current.Previous!;
               heightWalk--;
            }
         }

         return current;
      }
   }
}

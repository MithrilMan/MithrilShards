using System;
using System.Runtime.CompilerServices;
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
      /// Represents validity and availability statuses, used for example by <see cref="IsValid"/> and IsAvailable.
      /// </summary>
      private int status;

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
      public Target ChainWork { get; set; }

      /// <summary>
      /// Number of transactions in the chain up to and including this block.
      /// This value will be non-zero only if and only if transactions for this block and all its parents are available.
      /// Change to 64-bit type when necessary
      /// </value>
      /// <remarks>It's an in-memory value only that get computed during the header tree building and validation.</remarks>
      public uint ChainTxCount { get; set; }

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
         this.status = (int)HeaderValidityStatuses.ValidMask;
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
         this.status = (int)HeaderValidityStatuses.ValidTree;

         //pindexNew.TimeMax = (pindexNew->pprev ? std::max(pindexNew->pprev.TimeMax, pindexNew.Time) : pindexNew.Time);
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
      /// Check whether this entry is valid up to the passed validity level.
      /// </summary>
      /// <returns>
      ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
      /// </returns>
      public bool IsValid(HeaderValidityStatuses upTo = HeaderValidityStatuses.ValidTransactions)
      {
         // if some failed flag is on, it's not valid.
         if (IsInvalid()) return false;

         return (this.status & (int)HeaderValidityStatuses.ValidMask) >= (int)upTo;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool IsInvalid()
      {
         return (this.status & (int)HeaderValidityStatuses.FailedMask) != 0;
      }

      //! Raise the validity level of this block index entry.
      //! Returns true if the validity was changed.
      bool RaiseValidity(HeaderValidityStatuses upTo)
      {
         if ((status & (int)HeaderValidityStatuses.FailedMask) != 0) return false;

         if ((status & (int)HeaderValidityStatuses.ValidMask) < (int)upTo)
         {
            status = (status & ~(int)HeaderValidityStatuses.ValidMask) | (int)upTo;
            return true;
         }
         return false;
      }

      /// <summary>
      /// Check whether this entry has the required data availability.
      /// </summary>
      public bool HasAvailability(HeaderDataAvailability availability)
      {
         return (this.status & (int)availability) == (int)availability;
      }

      /// <summary>
      /// Check whether this entry has the required data availability.
      /// </summary>
      public void AddAvailability(HeaderDataAvailability availability)
      {
         this.status |= (int)availability;
      }

      /// <summary>
      /// Check whether this entry has the required data availability.
      /// </summary>
      public void RemoveAvailability(HeaderDataAvailability availability)
      {
         this.status &= ~(int)availability;
      }

      internal HeaderNode LastCommonAncestor(HeaderNode otherHeaderNode)
      {
         //move both chains at the height of the lower one
         HeaderNode? left = this.Height > otherHeaderNode.Height ? this.GetAncestor(otherHeaderNode.Height) : this;
         HeaderNode? right = otherHeaderNode.Height > this.Height ? otherHeaderNode.GetAncestor(this.Height) : otherHeaderNode;

         // walk back walking previous header, until we find that both are the header
         while (left != right && left != null && right != null)
         {
            left = left.Previous;
            right = right.Previous;
         }

         //at this point returning left or right is the same, both are equals and at worst case they go back down to genesis
         return left!;
      }

      public override string ToString()
      {
         return $"{this.Hash} ({this.Height})";
      }

      /// <summary>
      /// Determines whether <paramref name="expectedAncestor"/> is in same chain (can be this header itself).
      /// </summary>
      /// <param name="expectedAncestor">The expected ancestor.</param>
      /// <returns>
      ///   <c>true</c> if [is in same chain] [the specified expected ancestor]; otherwise, <c>false</c>.
      /// </returns>
      public bool IsInSameChain(HeaderNode expectedAncestor)
      {
         return this == expectedAncestor || this.GetAncestor(expectedAncestor.Height)?.Hash == expectedAncestor.Hash;
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

      public bool HaveTxsDownloaded()
      {
         return this.ChainTxCount > 0;
      }

      public override bool Equals(object? obj)
      {
         var item = obj as HeaderNode;
         if (item is null)
            return false;

         return this.Hash.Equals(item.Hash);
      }

      public static bool operator ==(HeaderNode? a, HeaderNode? b)
      {
         if (ReferenceEquals(a, b))
            return true;

         if (a is null || b is null)
            return false;

         return a.Hash == b.Hash;
      }

      /// <inheritdoc />
      public static bool operator !=(HeaderNode a, HeaderNode b)
      {
         return !(a == b);
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         return this.Hash.GetHashCode();
      }
   }
}

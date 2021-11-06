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
      private int _status;

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
      /// </summary>
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

         Hash = header.Hash;
         Height = 0;
         Previous = null;
         ChainWork = Target.Zero;
         Skip = null;
         _status = (int)HeaderValidityStatuses.ValidMask;
      }

      internal HeaderNode(BlockHeader header, HeaderNode previous)
      {
         if (header == null) ThrowHelper.ThrowArgumentNullException(nameof(header));
         if (header.Hash == null) ThrowHelper.ThrowArgumentException($"{nameof(header)} hash cannot be null.");

         Hash = header.Hash;
         Height = previous.Height + 1;
         Previous = previous;
         ChainWork = previous.ChainWork + new Target(header.Bits).GetBlockProof();
         Skip = previous.GetAncestor(GetSkipHeight(Height));
         _status = (int)HeaderValidityStatuses.ValidTree;

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
         static int invertLowestOne(int n)
         {
            return n & (n - 1);
         }

         // Determine which height to jump back to. Any number strictly lower than height is acceptable,
         // but the following expression seems to perform well in simulations (max 110 steps to go back
         // up to 2**18 blocks).
         return ((height & 1) != 0) ? invertLowestOne(invertLowestOne(height - 1)) + 1 : invertLowestOne(height);
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

         return (_status & (int)HeaderValidityStatuses.ValidMask) >= (int)upTo;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public bool IsInvalid()
      {
         return (_status & (int)HeaderValidityStatuses.FailedMask) != 0;
      }

      //! Raise the validity level of this block index entry.
      //! Returns true if the validity was changed.
      bool RaiseValidity(HeaderValidityStatuses upTo)
      {
         if ((_status & (int)HeaderValidityStatuses.FailedMask) != 0) return false;

         if ((_status & (int)HeaderValidityStatuses.ValidMask) < (int)upTo)
         {
            _status = (_status & ~(int)HeaderValidityStatuses.ValidMask) | (int)upTo;
            return true;
         }
         return false;
      }

      /// <summary>
      /// Check whether this entry has the required data availability.
      /// </summary>
      public bool HasAvailability(HeaderDataAvailability availability)
      {
         return (_status & (int)availability) == (int)availability;
      }

      /// <summary>
      /// Check whether this entry has the required data availability.
      /// </summary>
      public void AddAvailability(HeaderDataAvailability availability)
      {
         _status |= (int)availability;
      }

      /// <summary>
      /// Check whether this entry has the required data availability.
      /// </summary>
      public void RemoveAvailability(HeaderDataAvailability availability)
      {
         _status &= ~(int)availability;
      }

      internal HeaderNode LastCommonAncestor(HeaderNode otherHeaderNode)
      {
         //move both chains at the height of the lower one
         HeaderNode? left = Height > otherHeaderNode.Height ? GetAncestor(otherHeaderNode.Height) : this;
         HeaderNode? right = otherHeaderNode.Height > Height ? otherHeaderNode.GetAncestor(Height) : otherHeaderNode;

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
         return $"{Hash} ({Height})";
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
         return this == expectedAncestor || GetAncestor(expectedAncestor.Height)?.Hash == expectedAncestor.Hash;
      }

      /// <summary>
      /// Finds the ancestor of this entry in the chain that matches the given block height.
      /// </summary>
      /// <param name="height">The block height to search for.</param>
      /// <returns>The ancestor of this chain at the specified height.</returns>
      public HeaderNode? GetAncestor(int height)
      {
         if (height > Height || height < 0)
            return null;

         HeaderNode current = this;
         int heightWalk = Height;
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
         return ChainTxCount > 0;
      }

      public override bool Equals(object? obj)
      {
         if (obj is not HeaderNode item)
            return false;

         return Hash.Equals(item.Hash);
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
      public static bool operator !=(HeaderNode? a, HeaderNode? b)
      {
         return !(a == b);
      }

      /// <inheritdoc />
      public override int GetHashCode()
      {
         return Hash.GetHashCode();
      }
   }
}

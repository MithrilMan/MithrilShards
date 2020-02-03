using System;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// Represents a node in a linked list (tree) of headers.
   /// It exposes current node height and parent
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
      /// Total amount of work (expected number of hashes) in the chain up to and including this block.
      /// </summary>
      /// <remarks>It's an in-memory value only that get computed during the header tree building.</remarks>
      public Target ChainWork { get; internal set; }

      internal HeaderNode(int height, BlockHeader header, HeaderNode previous)
      {
         if (height < 0) ThrowHelper.ThrowArgumentException($"{nameof(height)} must be greater or equal to 0.");
         if (header == null) ThrowHelper.ThrowArgumentNullException(nameof(header));
         if (header.Hash == null) ThrowHelper.ThrowArgumentException($"{nameof(header)} hash cannot be null.");

         this.Height = height;
         this.Hash = header.Hash;
         this.Previous = previous;

         this.ChainWork = previous == null ? Target.Zero : (previous.ChainWork + new Target(header.Bits));
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
         if (height > this.Height)
            return null;

         //TODO: Improve using Skip list. this mean to add another field to this light class :/
         int heightDiff = this.Height - height;
         HeaderNode? ancestor = this;
         for (int i = 0; i < heightDiff; i++)
         {
            ancestor = ancestor!.Previous;
         }

         return ancestor;
      }
   }
}

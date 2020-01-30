using System;
using MithrilShards.Chain.Bitcoin.DataTypes;
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
      /// Gets the previous header identifier.
      /// </summary>
      /// <value>
      /// The previous header identifier.
      /// </value>
      public UInt256? PreviousHash { get; }

      /// <summary>
      /// Total amount of work (expected number of hashes) in the chain up to and including this block.
      /// </summary>
      /// <remarks>It's an in-memory value only that get computed during the header tree building.</remarks>
      public Target ChainWork { get; internal set; }

      public HeaderNode(int height, UInt256 hash, UInt256? previousHash, Target previousChainWork)
      {
         this.Height = height < 0 ? throw new ArgumentOutOfRangeException(nameof(height)) : height;
         this.Hash = hash ?? throw new ArgumentNullException(nameof(hash));
         this.PreviousHash = previousHash;
      }

      public HeaderNode(HeaderNode previousHeader int height, UInt256 hash, UInt256? previousHash, Target previousChainWork)
      {
         this.Height = height < 0 ? throw new ArgumentOutOfRangeException(nameof(height)) : height;
         this.Hash = hash ?? throw new ArgumentNullException(nameof(hash));
         this.PreviousHash = previousHash;
      }
   }
}

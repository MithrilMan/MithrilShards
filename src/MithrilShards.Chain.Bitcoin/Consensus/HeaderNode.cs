using System;
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

      public HeaderNode(int height, UInt256 hash, UInt256? previousHash)
      {
         this.Height = height < 0 ? throw new ArgumentOutOfRangeException(nameof(height)) : height;
         this.Hash = hash ?? throw new ArgumentNullException(nameof(hash));
         this.PreviousHash = previousHash;
      }

      /// <summary>
      /// Builds a new node that's logically after current node.
      /// </summary>
      /// <param name="newHash">The hash of the new header.</param>
      /// <returns></returns>
      public HeaderNode BuildNext(UInt256 newHash)
      {
         return new HeaderNode(this.Height + 1, newHash, this.Hash);
      }
   }
}

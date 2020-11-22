﻿using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// Represents a block header.
   /// </summary>
   public class BlockHeader
   {
      /// <summary>
      /// Block version information (note, this is signed)
      /// </summary>
      public int Version { get; set; }

      /// <summary>
      /// The hash value of the previous block this particular block references.
      /// </summary>
      public UInt256? PreviousBlockHash { get; set; }

      /// <summary>
      /// The reference to a Merkle tree collection which is a hash of all transactions related to this block.
      /// </summary>
      public UInt256? MerkleRoot { get; set; }

      /// <summary>
      /// A timestamp recording when this block was created (Will overflow in 2106).
      /// </summary>
      public uint TimeStamp { get; set; }

      /// <summary>
      /// The calculated difficulty target being used for this block.
      /// </summary>
      public uint Bits { get; set; }

      /// <summary>
      /// The nonce used to generate this block… to allow variations of the header and compute different hashes.
      /// </summary>
      public uint Nonce { get; set; }

      /// <summary>
      /// Not part of the protocol message, this property represents the block hash.
      /// Not used during serialization, it's computed externally when received from other peers.
      /// </summary>
      /// <remarks>
      /// This property is set when a header is received from a peer (or when it's mined).
      /// It's not part of the protocol message and doesn't participate to serialization.
      /// </remarks>
      public UInt256? Hash { get; set; }
   }
}

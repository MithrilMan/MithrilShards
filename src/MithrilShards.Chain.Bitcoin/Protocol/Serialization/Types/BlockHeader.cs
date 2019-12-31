﻿using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// Inventory vector (inv_vect).
   /// </summary>
   public class BlockHeader : ISerializableProtocolType<InventoryVector> {
      public string InternalName => "Block header";

      /// <summary>
      /// Block version information (note, this is signed)
      /// </summary>
      public int Version { get; set; }

      /// <summary>
      /// The hash value of the previous block this particular block references.
      /// </summary>
      public byte[] PreviousBlockHash { get; set; }

      /// <summary>
      /// The reference to a Merkle tree collection which is a hash of all transactions related to this block.
      /// </summary>
      public byte[] MerkleRoot { get; set; }

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


      public int Length => -1;

      public void Deserialize(ref SequenceReader<byte> data) {
         this.Version = data.ReadInt();
         this.PreviousBlockHash = data.ReadBytes(32);
         this.MerkleRoot = data.ReadBytes(32);
         this.TimeStamp = data.ReadUInt();
         this.Bits = data.ReadUInt();
         this.Nonce = data.ReadUInt();
      }

      public int Serialize(IBufferWriter<byte> writer) {
         int size = 0;
         size += writer.WriteInt(this.Version);
         size += writer.WriteBytes(this.PreviousBlockHash);
         size += writer.WriteBytes(this.MerkleRoot);
         size += writer.WriteUInt(this.TimeStamp);
         size += writer.WriteUInt(this.Bits);
         size += writer.WriteUInt(this.Nonce);

         return size;
      }
   }
}
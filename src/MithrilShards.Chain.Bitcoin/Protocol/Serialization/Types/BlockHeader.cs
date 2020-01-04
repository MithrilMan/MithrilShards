using System.Buffers;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types
{
   /// <summary>
   /// Inventory vector (inv_vect).
   /// </summary>
   public class BlockHeader : ISerializableProtocolType
   {
      /// <summary>
      /// Block version information (note, this is signed)
      /// </summary>
      public int Version { get; set; }

      /// <summary>
      /// The hash value of the previous block this particular block references.
      /// </summary>
      public UInt256 PreviousBlockHash { get; set; }

      /// <summary>
      /// The reference to a Merkle tree collection which is a hash of all transactions related to this block.
      /// </summary>
      public UInt256 MerkleRoot { get; set; }

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

      public ulong TransactionCount { get; set; }

      public void Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         this.Version = reader.ReadInt();
         this.PreviousBlockHash = reader.ReadUInt256();
         this.MerkleRoot = reader.ReadUInt256();
         this.TimeStamp = reader.ReadUInt();
         this.Bits = reader.ReadUInt();
         this.Nonce = reader.ReadUInt();
         this.TransactionCount = reader.ReadVarInt();
      }

      public int Serialize(IBufferWriter<byte> writer, int protocolVersion)
      {
         int size = 0;
         size += writer.WriteInt(this.Version);
         size += writer.WriteUInt256(this.PreviousBlockHash);
         size += writer.WriteUInt256(this.MerkleRoot);
         size += writer.WriteUInt(this.TimeStamp);
         size += writer.WriteUInt(this.Bits);
         size += writer.WriteUInt(this.Nonce);
         size += writer.WriteVarInt(this.TransactionCount);

         return size;
      }
   }
}

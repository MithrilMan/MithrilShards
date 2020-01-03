using System;
using System.Buffers;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// 32 bit unsigned integer (uint32_t).
   /// </summary>
   public class UInt256Type : ISerializableProtocolType<UInt256Type> {
      const int LENGTH = 32;
      public string InternalName => "uint256";
      public int Length => LENGTH;

      public UInt256 Value { get; set; }

      public void Deserialize(ref SequenceReader<byte> reader) {
         ReadOnlySequence<byte> sequence = reader.Sequence.Slice(reader.Position, LENGTH);
         if (sequence.IsSingleSegment) {
            this.Value = new UInt256(sequence.FirstSpan);
         }
         else {
            this.Value = new UInt256(sequence.ToArray());
         }
      }

      public int Serialize(IBufferWriter<byte> writer) {
         return writer.WriteUInt256(this.Value);
      }
   }
}

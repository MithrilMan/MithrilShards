using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// 16 bit unsigned integer (uint16_t).
   /// </summary>
   public class UShort : ISerializableProtocolTypeEndiannessAware<UShort> {
      public string InternalName => "uint16_t";

      public int Length => 2;

      public void Deserialize(ref SequenceReader<byte> reader, bool isLittleEndian) {
         throw new NotImplementedException();
      }

      public int Serialize(IBufferWriter<byte> writer, bool isLittleEndian) {
         throw new NotImplementedException();
      }
   }
}

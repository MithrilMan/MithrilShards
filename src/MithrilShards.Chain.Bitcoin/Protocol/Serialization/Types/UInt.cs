using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// 32 bit unsigned integer (uint32_t).
   /// </summary>
   public class UInt : ISerializableProtocolTypeEndiannessAware<UInt> {
      public string InternalName => "int32_t";
      public int Length => 4;

      public void Deserialize(SequenceReader<byte> data, bool isLittleEndian) {
         throw new NotImplementedException();
      }

      public int Serialize(IBufferWriter<byte> writer, bool isLittleEndian) {
         throw new NotImplementedException();
      }
   }
}

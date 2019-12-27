using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// 32 bit signed integer (int32_t).
   /// </summary>
   public class Int : ISerializableProtocolTypeEndiannessAware<Int> {
      public string InternalName => "int32_t";

      public int Length => 1;

      public void Deserialize(SequenceReader<byte> data, bool isLittleEndian) {
         throw new NotImplementedException();
      }

      public int Serialize(IBufferWriter<byte> writer, bool isLittleEndian) {
         throw new NotImplementedException();
      }
   }
}

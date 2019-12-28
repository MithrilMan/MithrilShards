using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// 64 bit signed integer (int64_t).
   /// </summary>
   public class Long : ISerializableProtocolTypeEndiannessAware<Long> {
      public string InternalName => "int64_t";

      public int Length => 8;

      public void Deserialize(ref SequenceReader<byte> data, bool isLittleEndian) {
         throw new NotImplementedException();
      }

      public int Serialize(IBufferWriter<byte> writer, bool isLittleEndian) {
         throw new NotImplementedException();
      }
   }
}

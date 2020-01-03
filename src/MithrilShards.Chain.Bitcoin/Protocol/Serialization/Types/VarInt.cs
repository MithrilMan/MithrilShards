using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// Variable number (var_int) aka CompactSize.
   /// </summary>
   public class VarInt : ISerializableProtocolType<VarInt> {
      public string InternalName => "var_int";

      public int Length => -1;

      public void Deserialize(ref SequenceReader<byte> reader) {
         throw new NotImplementedException();
      }

      public int Serialize(IBufferWriter<byte> writer) {
         throw new NotImplementedException();
      }
   }
}

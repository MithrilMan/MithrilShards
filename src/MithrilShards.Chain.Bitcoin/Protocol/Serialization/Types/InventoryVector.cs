using System;
using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   /// <summary>
   /// Inventory vector (inv_vect).
   /// </summary>
   public class InventoryVector : ISerializableProtocolType<InventoryVector> {
      public string InternalName => "inv_vect";

      public int Length => throw new NotImplementedException();

      public void Deserialize(SequenceReader<byte> data) {
         throw new NotImplementedException();
      }

      public int Serialize(IBufferWriter<byte> writer) {
         throw new NotImplementedException();
      }
   }
}

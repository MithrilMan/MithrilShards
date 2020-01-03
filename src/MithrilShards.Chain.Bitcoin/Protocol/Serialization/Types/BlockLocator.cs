using System.Buffers;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   public class BlockLocator : ISerializableProtocolType<BlockLocator> {
      public string InternalName => "block_locator";

      public int Length => -1;

      /// <summary>
      /// Block locator objects.
      /// Newest back to genesis block (dense to start, but then sparse)
      /// </summary>
      public UInt256Type[] BlockLocatorHashes { get; set; }

      public void Deserialize(ref SequenceReader<byte> reader) {

         this.BlockLocatorHashes = reader.ReadArray<UInt256Type>();

         //TODO: creare un metodo generico che accetta un metodo di serializzazione
         //this.BlockLocatorHashes = reader.ReadArray<UInt256Type>().Select(t => t.Value).ToArray();
         //this.BlockLocatorHashes = reader.ReadArray<UInt256Type>();
      }

      public int Serialize(IBufferWriter<byte> writer) {
         return writer.WriteArray(this.BlockLocatorHashes);
      }
   }
}

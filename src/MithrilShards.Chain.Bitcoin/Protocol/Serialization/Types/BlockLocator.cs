using System.Buffers;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types {
   public class BlockLocator : ISerializableProtocolType {
      /// <summary>
      /// Block locator objects.
      /// Newest back to genesis block (dense to start, but then sparse)
      /// </summary>
      public UInt256[] BlockLocatorHashes { get; set; }

      public void Deserialize(ref SequenceReader<byte> reader, int protocolVersion) {
         this.BlockLocatorHashes = reader.ReadArray(SequenceReaderExtensions.ReadUInt256);
      }

      public int Serialize(IBufferWriter<byte> writer, int protocolVersion) {
         return writer.WriteArray(this.BlockLocatorHashes, IBufferWriterExtensions.WriteUInt256);
      }
   }
}

using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages;
using MithrilShards.Core.Network.Protocol.Serialization;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Protocol.Serialization.Serializers.Messages
{
   public class WtxidRelayMessageSerializerTests
   {
      [Fact]
      public void Serialize_WtxidRelayMessage_IsEmpty()
      {
         // Arrange
         var message = new WtxidRelayMessage();
         var serializer = new WtxidRelayMessageSerializer();
         var bufferWriter = new ArrayBufferWriter<byte>();

         // Act
         serializer.Serialize(message, 0, bufferWriter);

         // Assert
         Assert.Empty(bufferWriter.WrittenSpan.ToArray());
      }

      [Fact]
      public void Deserialize_WtxidRelayMessage_ConsumesNothing()
      {
         // Arrange
         var message = new WtxidRelayMessage(); // Message to be populated
         var serializer = new WtxidRelayMessageSerializer();
         var emptySequence = new ReadOnlySequence<byte>(System.Array.Empty<byte>());
         var reader = new SequenceReader<byte>(emptySequence);

         // Act
         serializer.Deserialize(message, 0, ref reader);

         // Assert
         Assert.Equal(0, reader.Consumed);
         Assert.True(reader.End);
      }
   }
}

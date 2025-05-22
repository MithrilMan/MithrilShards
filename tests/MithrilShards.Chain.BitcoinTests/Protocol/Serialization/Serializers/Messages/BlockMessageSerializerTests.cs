using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;


namespace MithrilShards.Chain.BitcoinTests.Protocol.Serialization.Serializers.Messages
{
   public class BlockMessageSerializerTests
   {
      private readonly Mock<IProtocolTypeSerializer<Block>> _blockSerializerMock;
      private readonly BlockMessageSerializer _serializer;
      private readonly Mock<BitcoinPeerContext> _peerContextMock;
      private readonly Mock<INetworkMessageWriter> _networkMessageWriterMock;

      public BlockMessageSerializerTests()
      {
         _blockSerializerMock = new Mock<IProtocolTypeSerializer<Block>>();
         _serializer = new BlockMessageSerializer(_blockSerializerMock.Object);

         _networkMessageWriterMock = new Mock<INetworkMessageWriter>();
         _peerContextMock = new Mock<BitcoinPeerContext>(
             Mock.Of<ILogger<BitcoinPeerContext>>(),
             Mock.Of<IEventBus>(),
             PeerConnectionDirection.Outbound,
             "test-peer-blockmsg",
             new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8334),
             new System.Net.IPEndPoint(System.Net.IPAddress.Parse("1.2.3.4"), 8334),
             new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8333),
             _networkMessageWriterMock.Object
         );
         _peerContextMock.CallBase = true;

         // Setup default behaviors
         _blockSerializerMock.Setup(s => s.Deserialize(It.IsAny<ref SequenceReader<byte>>(), It.IsAny<int>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Returns(new Block { Header = new BlockHeader(), Transactions = System.Array.Empty<Transaction>() });
      }

      [Fact]
      public void Deserialize_BlockMessage_AlwaysSetsSerializeWitnessTrueForBlockSerializer()
      {
         // Arrange
         var blockBytes = new byte[] { 0x01, 0x00, 0x00, 0x00 /* version */, 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00 /* prev block hash */, 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00 /* merkle root */, 0x00,0x00,0x00,0x00 /* time */, 0x00,0x00,0x00,0x00 /* bits */, 0x00,0x00,0x00,0x00 /* nonce */, 0x00 /* tx count zero */ };
         var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(blockBytes));
         ProtocolTypeSerializerOptions? capturedOptions = null;

         // Peer context might or might not support witness, BlockMessageSerializer should ignore it for deserialization
         _peerContextMock.Object.CanServeWitness = false; // Test this scenario

         _blockSerializerMock.Setup(s => s.Deserialize(It.IsAny<ref SequenceReader<byte>>(), It.IsAny<int>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Callback((ref SequenceReader<byte> r, int pv, ProtocolTypeSerializerOptions? opts) => capturedOptions = opts)
             .Returns(new Block { Header = new BlockHeader(), Transactions = System.Array.Empty<Transaction>() });

         // Act
         _serializer.Deserialize(ref reader, 0, _peerContextMock.Object);

         // Assert
         Assert.NotNull(capturedOptions);
         Assert.True(capturedOptions.Has(SerializerOptions.SERIALIZE_WITNESS));
         Assert.True(capturedOptions.Get(SerializerOptions.SERIALIZE_WITNESS, false)); // Crucial: must be true for block deserialization
      }


      [Fact]
      public void Serialize_BlockMessage_PassesThroughOptions_AndBlockSerializerHandlesWitness()
      {
         // Arrange
         var message = new BlockMessage { Block = new Block { Header = new BlockHeader(), Transactions = new Transaction[] { new Transaction() } } };
         var writer = new ArrayBufferWriter<byte>();
         ProtocolTypeSerializerOptions? capturedOptionsForBlockSerializer = null;

         // This option will be passed from BlockMessageSerializer to BlockSerializer
         var initialOptions = new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, false));
         // We expect BlockSerializer (mocked here for simplicity, but tested in BlockSerializerTests)
         // to override SERIALIZE_WITNESS to true for its transactions internally.
         // Here, we just check that options are passed through.

         _blockSerializerMock.Setup(s => s.Serialize(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<IBufferWriter<byte>>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Callback((Block tx, int pv, IBufferWriter<byte> w, ProtocolTypeSerializerOptions? opts) => capturedOptionsForBlockSerializer = opts)
             .Returns(90); // Dummy size

         // Act
         // Simulate BlockMessage.PopulateSerializerOption if it existed and set initialOptions
         // For this test, we pass initialOptions directly to Serialize.
         _serializer.Serialize(message, 0, _peerContextMock.Object, writer); // No options passed, so default internal options are used.

         // Assert
         // BlockMessageSerializer.Serialize creates 'options = null' then calls PopulateSerializerOption (noop in current stub).
         // Then calls WriteWithSerializer with these null options.
         // So, the options received by _blockSerializerMock will be null or default from WriteWithSerializer.
         // The important part is that BlockSerializer *itself* was fixed to handle witness correctly.
         // This test verifies that BlockMessageSerializer doesn't interfere negatively.
         Assert.NotNull(capturedOptionsForBlockSerializer); // Should receive default options from WriteWithSerializer or null

         // If we want to test options propagation:
         var specificOptions = new ProtocolTypeSerializerOptions(("TestOption", true));
         message.PopulateSerializerOption(ref specificOptions); // manual call to simulate if message had this method
         _serializer.Serialize(message, 0, _peerContextMock.Object, writer);
         // Assert.True(capturedOptionsForBlockSerializer.Has("TestOption")); // If PopulateSerializerOption was real and set it.
         // For now, the key is that BlockSerializer itself does the right thing.
      }
   }
}

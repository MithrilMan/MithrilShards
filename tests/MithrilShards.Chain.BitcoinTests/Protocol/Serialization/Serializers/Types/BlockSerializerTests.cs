using System.Buffers;
using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;
using Moq;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Protocol.Serialization.Serializers.Types
{
   public class BlockSerializerTests
   {
      private readonly Mock<IProtocolTypeSerializer<BlockHeader>> _headerSerializerMock;
      private readonly Mock<IProtocolTypeSerializer<Transaction>> _transactionSerializerMock;
      private readonly BlockSerializer _serializer;

      public BlockSerializerTests()
      {
         _headerSerializerMock = new Mock<IProtocolTypeSerializer<BlockHeader>>();
         _transactionSerializerMock = new Mock<IProtocolTypeSerializer<Transaction>>();
         _serializer = new BlockSerializer(_headerSerializerMock.Object, _transactionSerializerMock.Object);

         // Setup default behaviors for mocks
         _headerSerializerMock.Setup(s => s.Deserialize(It.IsAny<ref SequenceReader<byte>>(), It.IsAny<int>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Returns(new BlockHeader()); // Return a dummy header
         _headerSerializerMock.Setup(s => s.Serialize(It.IsAny<BlockHeader>(), It.IsAny<int>(), It.IsAny<IBufferWriter<byte>>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Returns(80); // Dummy size for header

         _transactionSerializerMock.Setup(s => s.Deserialize(It.IsAny<ref SequenceReader<byte>>(), It.IsAny<int>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Returns(new Transaction()); // Return a dummy transaction
         _transactionSerializerMock.Setup(s => s.Serialize(It.IsAny<Transaction>(), It.IsAny<int>(), It.IsAny<IBufferWriter<byte>>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Returns(10); // Dummy size for transaction
      }

      [Fact]
      public void Deserialize_Block_PassesSerializeWitnessTrueToTransactionSerializer()
      {
         // Arrange
         // Construct a minimal block byte sequence.
         // Header (mocked, 0 bytes consumed by mock for this byte array)
         // TxCount = 1 (0x01)
         // Transaction (mocked, 0 bytes consumed by mock for this byte array)
         var blockBytes = new byte[] { 0x01 }; // Just tx count, header and tx are mocked to consume nothing from stream for this test
         var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(blockBytes));
         ProtocolTypeSerializerOptions? capturedOptions = null;

         // Capture options passed to transactionSerializer.Deserialize via ReadArray extension
         // This requires a more elaborate setup for ReadArray or direct test of _transactionSerializer.Deserialize
         // For simplicity, we rely on the fact that BlockSerializer creates txOptions and passes it to ReadArray,
         // and ReadArray will pass it to _transactionSerializer.Deserialize for each item.
         // We verify the options created within BlockSerializer.Deserialize.

         _transactionSerializerMock.Setup(s => s.Deserialize(It.IsAny<ref SequenceReader<byte>>(), It.IsAny<int>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Callback((ref SequenceReader<byte> r, int pv, ProtocolTypeSerializerOptions? opts) => capturedOptions = opts)
             .Returns(new Transaction());

         // Act
         _serializer.Deserialize(ref reader, 0, null); // options passed to BlockSerializer itself can be null

         // Assert
         Assert.NotNull(capturedOptions);
         Assert.True(capturedOptions.Has(SerializerOptions.SERIALIZE_WITNESS));
         Assert.True(capturedOptions.Get(SerializerOptions.SERIALIZE_WITNESS, false));
      }

      [Fact]
      public void Serialize_Block_PassesSerializeWitnessTrueToTransactionSerializer()
      {
         // Arrange
         var block = new Block
         {
            Header = new BlockHeader(),
            Transactions = new Transaction[] { new Transaction() }
         };
         var writer = new ArrayBufferWriter<byte>();
         ProtocolTypeSerializerOptions? capturedOptions = null;

         _transactionSerializerMock.Setup(s => s.Serialize(It.IsAny<Transaction>(), It.IsAny<int>(), It.IsAny<IBufferWriter<byte>>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Callback((Transaction tx, int pv, IBufferWriter<byte> w, ProtocolTypeSerializerOptions? opts) => capturedOptions = opts)
             .Returns(10);


         // Act
         _serializer.Serialize(block, 0, writer, null); // options passed to BlockSerializer itself can be null

         // Assert
         Assert.NotNull(capturedOptions);
         Assert.True(capturedOptions.Has(SerializerOptions.SERIALIZE_WITNESS));
         Assert.True(capturedOptions.Get(SerializerOptions.SERIALIZE_WITNESS, false));
      }

      // More detailed tests would involve actual byte streams for blocks with various transaction types
      // and verifying the full deserialization and serialization.
      // However, that would heavily depend on non-mocked sub-serializers or very complex mock setups.
      // These tests focus on the critical part: BlockSerializer passing the correct options.
   }
}

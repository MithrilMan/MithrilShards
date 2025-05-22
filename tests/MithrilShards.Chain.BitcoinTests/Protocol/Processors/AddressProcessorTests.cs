using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Processors;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerBehaviorManager;
using Moq;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Protocol.Processors
{
   public class AddressProcessorTests
   {
      private readonly Mock<ILogger<AddressProcessor>> _loggerMock;
      private readonly Mock<IEventBus> _eventBusMock;
      private readonly Mock<IPeerBehaviorManager> _peerBehaviorManagerMock;
      private Mock<BitcoinPeerContext> _bitcoinPeerContextMock;
      private Mock<INetworkMessageWriter> _networkMessageWriterMock;
      // private Mock<IPeerAddressBook> _peerAddressBookMock; // Uncomment if IPeerAddressBook is used

      public AddressProcessorTests()
      {
         _loggerMock = new Mock<ILogger<AddressProcessor>>();
         _eventBusMock = new Mock<IEventBus>();
         _peerBehaviorManagerMock = new Mock<IPeerBehaviorManager>();
         _networkMessageWriterMock = new Mock<INetworkMessageWriter>();
         // _peerAddressBookMock = new Mock<IPeerAddressBook>();

         // Setup BitcoinPeerContext mock
         _bitcoinPeerContextMock = new Mock<BitcoinPeerContext>(
             Mock.Of<ILogger<BitcoinPeerContext>>(),
             _eventBusMock.Object,
             PeerConnectionDirection.Outbound,
             "test-peer-addr",
             new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8334),
             new System.Net.IPEndPoint(System.Net.IPAddress.Parse("1.2.3.4"), 8334),
             new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8333),
             _networkMessageWriterMock.Object
         );
         _bitcoinPeerContextMock.CallBase = true;
      }

      private AddressProcessor CreateProcessor()
      {
         var processor = new AddressProcessor(
             _loggerMock.Object,
             _eventBusMock.Object,
             _peerBehaviorManagerMock.Object
             // _peerAddressBookMock.Object // Uncomment if IPeerAddressBook is used
         );

         processor.GetType().GetMethod("AttachPeer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
Invoke(processor, new object[] { _bitcoinPeerContextMock.Object });

         return processor;
      }

      [Fact]
      public async Task ProcessMessageAsync_GetAddrMessage_PeerSupportsAddrV2_SendsAddrv2Message()
      {
         // Arrange
         var processor = CreateProcessor();
         _bitcoinPeerContextMock.Object.SupportsAddrv2 = true;
         var getAddrMessage = new GetAddrMessage();
         INetworkMessage? sentMessage = null;

         _networkMessageWriterMock.Setup(w => w.WriteAsync(It.IsAny<INetworkMessage>(), It.IsAny<CancellationToken>()))
             .Callback<INetworkMessage, CancellationToken>((msg, ct) => sentMessage = msg)
             .Returns(ValueTask.CompletedTask);

         // Act
         await (processor as INetworkMessageHandler<GetAddrMessage>).ProcessMessageAsync(getAddrMessage, CancellationToken.None);

         // Assert
         Assert.NotNull(sentMessage);
         Assert.IsType<Addrv2Message>(sentMessage);
         // Further assertions can be made if PeerAddressBook was mocked to return specific addresses
         Assert.Empty(((Addrv2Message)sentMessage).Addresses); // Currently sends empty list
      }

      [Fact]
      public async Task ProcessMessageAsync_GetAddrMessage_PeerDoesNotSupportAddrV2_SendsAddrMessage()
      {
         // Arrange
         var processor = CreateProcessor();
         _bitcoinPeerContextMock.Object.SupportsAddrv2 = false;
         var getAddrMessage = new GetAddrMessage();
         INetworkMessage? sentMessage = null;

         _networkMessageWriterMock.Setup(w => w.WriteAsync(It.IsAny<INetworkMessage>(), It.IsAny<CancellationToken>()))
             .Callback<INetworkMessage, CancellationToken>((msg, ct) => sentMessage = msg)
             .Returns(ValueTask.CompletedTask);

         // Act
         await (processor as INetworkMessageHandler<GetAddrMessage>).ProcessMessageAsync(getAddrMessage, CancellationToken.None);

         // Assert
         Assert.NotNull(sentMessage);
         Assert.IsType<AddrMessage>(sentMessage);
         Assert.Empty(((AddrMessage)sentMessage).Addresses); // Currently sends empty list
      }

      [Fact]
      public async Task ProcessMessageAsync_Addrv2Message_LogsReceivedAddresses()
      {
         // Arrange
         var processor = CreateProcessor();
         var addresses = new List<NetworkAddressV2>
            {
                new NetworkAddressV2 { Time = 123, Services = NodeServices.Network, NetworkId = BIP155NetworkId.IPV4, Address = new byte[]{1,2,3,4}, Port = 8333 }
            };
         var addrv2Message = new Addrv2Message { Addresses = addresses };

         // Act
         await (processor as INetworkMessageHandler<Addrv2Message>).ProcessMessageAsync(addrv2Message, CancellationToken.None);

         // Assert
         // Verify logging. This requires a more complex logger setup to capture logs.
         // For now, this test ensures the method runs without error.
         // If PeerAddressBook was used, verify calls to it.
         _loggerMock.Verify(
             x => x.Log(
                 LogLevel.Debug,
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received Addrv2Message with 1 addresses.")),
                 null,
                 It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
             Times.Once);

         _loggerMock.Verify(
             x => x.Log(
                 LogLevel.Trace, // As per current implementation in AddressProcessor
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Addrv2: NetID IPV4, Addr 1.2.3.4:8333")),
                 null,
                 It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
             Times.Once);
      }

      [Fact]
      public async Task ProcessMessageAsync_AddrMessage_LogsReceivedAddresses()
      {
         // Arrange
         var processor = CreateProcessor();
         var addresses = new NetworkAddress[]
            {
                new NetworkAddress(NodeServices.Network, new System.Net.IPEndPoint(System.Net.IPAddress.Parse("5.6.7.8"), 8333), 456)
            };
         var addrMessage = new AddrMessage { Addresses = addresses };

         // Act
         await (processor as INetworkMessageHandler<AddrMessage>).ProcessMessageAsync(addrMessage, CancellationToken.None);

         // Assert
         _loggerMock.Verify(
             x => x.Log(
                 LogLevel.Debug,
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received AddrMessage with 1 addresses.")),
                 null,
                 It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
             Times.Once);

         _loggerMock.Verify(
             x => x.Log(
                 LogLevel.Trace, // As per current implementation in AddressProcessor
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Legacy Addr: 5.6.7.8:8333")),
                 null,
                 It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
             Times.Once);
      }
   }
}

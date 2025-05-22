using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Processors;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerBehaviorManager;
using Moq;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Protocol.Processors
{
   public class HandshakeProcessorTests
   {
      private readonly Mock<ILogger<HandshakeProcessor>> _loggerMock;
      private readonly Mock<IEventBus> _eventBusMock;
      private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
      private readonly Mock<IRandomNumberGenerator> _randomNumberGeneratorMock;
      private readonly NodeImplementation _nodeImplementation; // Real instance
      private readonly Mock<IPeerBehaviorManager> _peerBehaviorManagerMock;
      private readonly Mock<IInitialBlockDownloadTracker> _ibdTrackerMock;
      private readonly Mock<IUserAgentBuilder> _userAgentBuilderMock;
      private readonly Mock<ILocalServiceProvider> _localServiceProviderMock;
      private readonly Mock<IHeadersTree> _headersTreeMock;
      private readonly Mock<SelfConnectionTracker> _selfConnectionTrackerMock;
      private Mock<BitcoinPeerContext> _bitcoinPeerContextMock; // Changed to allow setting SupportsAddrv2 etc.
      private Mock<INetworkMessageWriter> _networkMessageWriterMock;


      public HandshakeProcessorTests()
      {
         _loggerMock = new Mock<ILogger<HandshakeProcessor>>();
         _eventBusMock = new Mock<IEventBus>();
         _dateTimeProviderMock = new Mock<IDateTimeProvider>();
         _randomNumberGeneratorMock = new Mock<IRandomNumberGenerator>();

         // Setup NodeImplementation as it's configured in ForgeBuilderExtensions after Task 1.1
         _nodeImplementation = new NodeImplementation(KnownVersion.V70012, KnownVersion.CurrentVersion);

         _peerBehaviorManagerMock = new Mock<IPeerBehaviorManager>();
         _ibdTrackerMock = new Mock<IInitialBlockDownloadTracker>();
         _userAgentBuilderMock = new Mock<IUserAgentBuilder>();
         _localServiceProviderMock = new Mock<ILocalServiceProvider>();
         _headersTreeMock = new Mock<IHeadersTree>();
         _selfConnectionTrackerMock = new Mock<SelfConnectionTracker>();
         _networkMessageWriterMock = new Mock<INetworkMessageWriter>();

         // Setup BitcoinPeerContext mock
         // Arguments for BitcoinPeerContext: ILogger logger, IEventBus eventBus, PeerConnectionDirection direction, string peerId, EndPoint localEndPoint, EndPoint publicEndPoint, EndPoint remoteEndPoint, INetworkMessageWriter messageWriter
         // Most of these can be default or null for basic tests if not directly used by the properties we are testing.
         // However, SendMessageAsync will use the messageWriter.
         _bitcoinPeerContextMock = new Mock<BitcoinPeerContext>(
             Mock.Of<ILogger<BitcoinPeerContext>>(),
             _eventBusMock.Object, // can reuse
             PeerConnectionDirection.Outbound, // default, can be changed per test
             "test-peer",
             new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8334), // local
             new System.Net.IPEndPoint(System.Net.IPAddress.Parse("1.2.3.4"), 8334), // public
             new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8333), // remote
             _networkMessageWriterMock.Object // message writer
         );
         _bitcoinPeerContextMock.CallBase = true; // Important for properties to work if not overridden


         // Setup default return values for mocks
         _localServiceProviderMock.Setup(lsp => lsp.GetServices()).Returns(NodeServices.Network | NodeServices.Witness);
         _userAgentBuilderMock.Setup(uab => uab.GetUserAgent()).Returns("test-agent");
         var fakeTip = new HeaderNode(new BlockHeader { Hash = new Core.DataTypes.UInt256(new byte[32]) }, 0); // Minimal tip
         _headersTreeMock.Setup(ht => ht.GetTip()).Returns(fakeTip);
         _dateTimeProviderMock.Setup(dtp => dtp.GetTimeOffset()).Returns(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
         _randomNumberGeneratorMock.Setup(rng => rng.GetUint64()).Returns(12345UL);
      }

      private HandshakeProcessor CreateProcessor(PeerConnectionDirection direction = PeerConnectionDirection.Outbound)
      {
         // Reset and reconfigure direction for peer context if needed
         _networkMessageWriterMock.Reset(); // Reset call counts for writer
         _bitcoinPeerContextMock = new Mock<BitcoinPeerContext>(
             Mock.Of<ILogger<BitcoinPeerContext>>(),
             _eventBusMock.Object,
             direction, // Use specified direction
             "test-peer",
             new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8334),
             new System.Net.IPEndPoint(System.Net.IPAddress.Parse("1.2.3.4"), 8334),
             new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8333),
             _networkMessageWriterMock.Object
         );
         _bitcoinPeerContextMock.CallBase = true;


         var processor = new HandshakeProcessor(
             _loggerMock.Object,
             _eventBusMock.Object,
             _dateTimeProviderMock.Object,
             _randomNumberGeneratorMock.Object,
             _nodeImplementation, // Use the real instance
             _peerBehaviorManagerMock.Object,
             _ibdTrackerMock.Object,
             _userAgentBuilderMock.Object,
             _localServiceProviderMock.Object,
             _headersTreeMock.Object,
             _selfConnectionTrackerMock.Object
         );

         // BaseProcessor.AttachPeer needs to be called to set PeerContext
         // We use the specific BitcoinPeerContext mock instance here.
         processor.GetType().GetMethod("AttachPeer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
Invoke(processor, new object[] { _bitcoinPeerContextMock.Object });

         return processor;
      }

      [Fact]
      public async Task OnPeerAttachedAsync_Outbound_SendsVersionAndSendAddrV2AndWtxidRelay()
      {
         // Arrange
         var processor = CreateProcessor(PeerConnectionDirection.Outbound);
         var sentMessages = new List<INetworkMessage>();
         _networkMessageWriterMock.Setup(w => w.WriteAsync(It.IsAny<INetworkMessage>(), It.IsAny<CancellationToken>()))
             .Callback<INetworkMessage, CancellationToken>((msg, ct) => sentMessages.Add(msg))
             .Returns(ValueTask.CompletedTask);


         // Act
         // OnPeerAttachedAsync is called when AttachPeer is invoked, which is done in CreateProcessor
         // For outbound, it should send Version then SendAddrV2 then WtxidRelay
         // To re-trigger OnPeerAttachedAsync for this test's specific context capture if CreateProcessor isn't enough:
         var onPeerAttachedMethod = typeof(HandshakeProcessor).GetMethod("OnPeerAttachedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
         await (ValueTask)onPeerAttachedMethod!.Invoke(processor, null)!;


         // Assert
         Assert.Equal(3, sentMessages.Count); // Version, SendAddrv2, WtxidRelay
         Assert.IsType<VersionMessage>(sentMessages[0]);
         Assert.IsType<SendAddrv2Message>(sentMessages[1]);
         Assert.IsType<WtxidRelayMessage>(sentMessages[2]); // WtxidRelay is sent after SendAddrV2
         _networkMessageWriterMock.Verify(w => w.WriteAsync(It.IsAny<VersionMessage>(), It.IsAny<CancellationToken>()), Times.Once);
         _networkMessageWriterMock.Verify(w => w.WriteAsync(It.IsAny<SendAddrv2Message>(), It.IsAny<CancellationToken>()), Times.Once);
         _networkMessageWriterMock.Verify(w => w.WriteAsync(It.IsAny<WtxidRelayMessage>(), It.IsAny<CancellationToken>()), Times.Once);
      }


      [Fact]
      public async Task ProcessMessageAsync_VersionMessage_Inbound_SendsVersionAndSendAddrV2AndWtxidRelayAndVerack()
      {
         // Arrange
         var processor = CreateProcessor(PeerConnectionDirection.Inbound);
         var peerVersionMessage = new VersionMessage { Version = KnownVersion.CurrentVersion, Services = (ulong)NodeServices.Network };
         var sentMessages = new List<INetworkMessage>();

         _networkMessageWriterMock.Setup(w => w.WriteAsync(It.IsAny<INetworkMessage>(), It.IsAny<CancellationToken>()))
             .Callback<INetworkMessage, CancellationToken>((msg, ct) => sentMessages.Add(msg))
             .Returns(ValueTask.CompletedTask);

         // Act
         await (processor as INetworkMessageHandler<VersionMessage>).ProcessMessageAsync(peerVersionMessage, CancellationToken.None);

         // Assert
         Assert.Equal(4, sentMessages.Count); // Our Version, SendAddrv2, WtxidRelay, Verack
         Assert.IsType<VersionMessage>(sentMessages[0]);
         Assert.IsType<SendAddrv2Message>(sentMessages[1]);
         Assert.IsType<WtxidRelayMessage>(sentMessages[2]);
         Assert.IsType<VerackMessage>(sentMessages[3]);
      }


      [Fact]
      public async Task ProcessMessageAsync_SendAddrv2Message_SetsSupportsAddrv2Flag()
      {
         // Arrange
         var processor = CreateProcessor();
         var sendAddrv2Message = new SendAddrv2Message();
         _bitcoinPeerContextMock.Object.SupportsAddrv2 = false; // Ensure it's initially false

         // Act
         await (processor as INetworkMessageHandler<SendAddrv2Message>).ProcessMessageAsync(sendAddrv2Message, CancellationToken.None);

         // Assert
         Assert.True(_bitcoinPeerContextMock.Object.SupportsAddrv2);
      }

      [Fact]
      public async Task ProcessMessageAsync_WtxidRelayMessage_SetsSupportsWtxidRelayFlag()
      {
         // Arrange
         var processor = CreateProcessor();
         var wtxidRelayMessage = new WtxidRelayMessage();
         _bitcoinPeerContextMock.Object.SupportsWtxidRelay = false; // Ensure it's initially false

         // Act
         await (processor as INetworkMessageHandler<WtxidRelayMessage>).ProcessMessageAsync(wtxidRelayMessage, CancellationToken.None);

         // Assert
         Assert.True(_bitcoinPeerContextMock.Object.SupportsWtxidRelay);
      }

      [Fact]
      public void CreateVersionMessage_SetsVersionCorrectly()
      {
         // Arrange
         var processor = CreateProcessor();
         int expectedVersion = KnownVersion.CurrentVersion; // Should be V70016

         // Act
         // Access CreateVersionMessage via reflection as it's private
         var methodInfo = typeof(HandshakeProcessor).GetMethod("CreateVersionMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
         var versionMessage = methodInfo?.Invoke(processor, null) as VersionMessage;

         // Assert
         Assert.NotNull(versionMessage);
         Assert.Equal(expectedVersion, versionMessage!.Version);
      }

      [Fact]
      public void CreateVersionMessage_SetsServicesCorrectly()
      {
         // Arrange
         var processor = CreateProcessor();
         NodeServices expectedServices = NodeServices.Network | NodeServices.Witness; // Default from constructor
         _localServiceProviderMock.Setup(lsp => lsp.GetServices()).Returns(expectedServices); // ensure mock returns this

         // Act
         var methodInfo = typeof(HandshakeProcessor).GetMethod("CreateVersionMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
         var versionMessage = methodInfo?.Invoke(processor, null) as VersionMessage;

         // Assert
         Assert.NotNull(versionMessage);
         Assert.Equal((ulong)expectedServices, versionMessage!.Services);
         _localServiceProviderMock.Verify(lsp => lsp.GetServices(), Times.AtLeastOnce()); // Called during CreateVersionMessage
      }
   }
}

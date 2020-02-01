using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Network.Legacy
{

   public class PeerConnectionFactory : IPeerConnectionFactory
   {
      /// <summary>Instance logger.</summary>
      private readonly ILogger logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEventBus eventBus;

      /// <summary>Provider of time functions.</summary>
      private readonly IDateTimeProvider dateTimeProvider;
      readonly IPeerContextFactory peerContextFactory;
      readonly INetworkDefinition chainDefinition;
      private readonly INetworkMessageProcessorFactory networkMessageProcessorFactory;
      readonly INetworkMessageSerializerManager networkMessageSerializerManager;

      public PeerConnectionFactory(ILoggerFactory loggerFactory,
                                   IEventBus eventBus,
                                   IDateTimeProvider dateTimeProvider,
                                   IPeerContextFactory peerContextFactory,
                                   INetworkDefinition chainDefinition,
                                   INetworkMessageProcessorFactory networkMessageProcessorFactory,
                                   INetworkMessageSerializerManager networkMessageSerializerManager)
      {
         this.loggerFactory = loggerFactory;
         this.eventBus = eventBus;
         this.dateTimeProvider = dateTimeProvider;
         this.peerContextFactory = peerContextFactory;
         this.chainDefinition = chainDefinition;
         this.networkMessageProcessorFactory = networkMessageProcessorFactory;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.logger = loggerFactory.CreateLogger<PeerConnectionFactory>();
      }

      public IPeerConnection CreatePeerConnection(TcpClient connectingPeer, CancellationToken cancellationToken)
      {
         var peer = new PeerConnection(
            this.loggerFactory.CreateLogger<PeerConnection>(),
            this.eventBus,
            this.dateTimeProvider,
            connectingPeer,
            PeerConnectionDirection.Inbound,
            this.peerContextFactory,
            new NetworkMessageDecoder(this.loggerFactory.CreateLogger<NetworkMessageDecoder>(),
                                      this.chainDefinition,
                                      this.networkMessageSerializerManager,
                                      new ConnectionContextData(this.chainDefinition.MagicBytes)),
            this.networkMessageProcessorFactory,
            cancellationToken
            );

         return peer;
      }
   }
}

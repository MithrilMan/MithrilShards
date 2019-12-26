using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;

namespace MithrilShards.Network.Network {

   public class PeerConnectionFactory : IPeerConnectionFactory {
      /// <summary>Instance logger.</summary>
      private readonly ILogger logger;
      readonly ILoggerFactory loggerFactory;
      readonly IEventBus eventBus;

      /// <summary>Provider of time functions.</summary>
      private readonly IDateTimeProvider dateTimeProvider;
      readonly IPeerContextFactory peerContextFactory;

      public PeerConnectionFactory(ILoggerFactory loggerFactory, IEventBus eventBus, IDateTimeProvider dateTimeProvider, IPeerContextFactory peerContextFactory) {
         this.loggerFactory = loggerFactory;
         this.eventBus = eventBus;
         this.dateTimeProvider = dateTimeProvider;
         this.peerContextFactory = peerContextFactory;
         this.logger = loggerFactory.CreateLogger<PeerConnectionFactory>();
      }

      public IPeerConnection CreatePeerConnection(TcpClient connectingPeer, CancellationToken cancellationToken) {
         var peer = new PeerConnection(
            this.loggerFactory.CreateLogger<PeerConnection>(),
            this.eventBus,
            this.dateTimeProvider,
            connectingPeer,
            PeerConnectionDirection.Inbound,
            this.peerContextFactory,
            cancellationToken
            );

         return peer;
      }
   }
}

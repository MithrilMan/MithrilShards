using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.P2P.Network.StateMachine;

namespace MithrilShards.P2P.Network {

   public class PeerConnectionFactory : IPeerConnectionFactory {
      /// <summary>Instance logger.</summary>
      private readonly ILogger logger;
      readonly ILoggerFactory loggerFactory;

      /// <summary>Provider of time functions.</summary>
      private readonly IDateTimeProvider dateTimeProvider;

      public PeerConnectionFactory(ILoggerFactory loggerFactory, IDateTimeProvider dateTimeProvider) {
         this.loggerFactory = loggerFactory;
         this.dateTimeProvider = dateTimeProvider;

         this.logger = loggerFactory.CreateLogger<PeerConnectionFactory>();
      }

      public async Task AcceptConnectionAsync(TcpClient connectingPeer, CancellationToken cancellationToken) {
         var peer = new PeerConnection(
            this.loggerFactory.CreateLogger<PeerConnection>(),
            this.dateTimeProvider,
            connectingPeer,
            PeerConnectionDirection.Inbound,
            cancellationToken
            );

         await peer.ConnectAsync(cancellationToken);
      }
   }
}

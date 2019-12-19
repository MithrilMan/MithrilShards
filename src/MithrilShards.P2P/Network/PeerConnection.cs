using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.P2P.Network.StateMachine;

namespace MithrilShards.P2P.Network {

   public class PeerConnection {
      /// <summary>Instance logger.</summary>
      private readonly ILogger logger;
      readonly IEventBus eventBus;

      /// <summary>Provider of time functions.</summary>
      private readonly IDateTimeProvider dateTimeProvider;
      readonly TcpClient connectedClient;
      private readonly PeerConnectionDirection peerConnectionDirection;
      readonly CancellationToken cancellationToken;
      readonly PeerConnectionStateMachine connectionStateMachine;

      public TimeSpan? TimeOffset { get; private set; }

      public PeerDisconnectionReason DisconnectReason { get; private set; }

      public PeerConnection(ILogger<PeerConnection> logger,
                            IEventBus eventBus,
                            IDateTimeProvider dateTimeProvider,
                            TcpClient connectedClient,
                            PeerConnectionDirection peerConnectionDirection,
                            CancellationToken cancellationToken) {
         this.logger = logger;
         this.eventBus = eventBus;
         this.dateTimeProvider = dateTimeProvider;
         this.connectedClient = connectedClient;
         this.peerConnectionDirection = peerConnectionDirection;
         this.cancellationToken = cancellationToken;

         this.connectionStateMachine = new PeerConnectionStateMachine(logger, eventBus, peerConnectionDirection, connectedClient, cancellationToken);
      }

      /// <inheritdoc/>
      public async Task ConnectAsync(CancellationToken cancellation = default(CancellationToken)) {
            await this.connectionStateMachine.AcceptOutgoingConnection();
      }
   }
}

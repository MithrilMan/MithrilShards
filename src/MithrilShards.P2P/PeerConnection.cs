﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Network.Legacy.StateMachine;

namespace MithrilShards.Network.Legacy
{

   public class PeerConnection : IPeerConnection, INetworkMessageWriter
   {
      /// <summary>Instance logger.</summary>
      private readonly ILogger logger;
      readonly IEventBus eventBus;

      /// <summary>Provider of time functions.</summary>
      readonly IPeerContextFactory peerContextFactory;
      readonly NetworkMessageDecoder networkMessageDecoder;
      private readonly INetworkMessageProcessorFactory networkMessageProcessorFactory;

      internal TcpClient ConnectedClient { get; }
      public PeerConnectionDirection Direction { get; }

      readonly CancellationToken cancellationToken;

      public IPeerContext PeerContext { get; }

      readonly PeerConnectionStateMachine connectionStateMachine;

      public TimeSpan? TimeOffset { get; private set; }

      public PeerDisconnectionReason? DisconnectReason { get; private set; }

      public PeerConnection(ILogger<PeerConnection> logger,
                            IEventBus eventBus,
                            TcpClient connectedClient,
                            PeerConnectionDirection peerConnectionDirection,
                            IPeerContextFactory peerContextFactory,
                            NetworkMessageDecoder networkMessageDecoder,
                            INetworkMessageProcessorFactory networkMessageProcessorFactory,
                            CancellationToken cancellationToken)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.ConnectedClient = connectedClient;
         this.Direction = peerConnectionDirection;
         this.peerContextFactory = peerContextFactory;
         this.networkMessageDecoder = networkMessageDecoder;
         this.networkMessageProcessorFactory = networkMessageProcessorFactory;
         this.cancellationToken = cancellationToken;

         this.PeerContext = this.peerContextFactory.Create(
            PeerConnectionDirection.Inbound,
            Guid.NewGuid().ToString(),
            connectedClient.Client.LocalEndPoint,
            connectedClient.Client.RemoteEndPoint,
            this);

         this.networkMessageDecoder.SetPeerContext(this.PeerContext);

         this.connectionStateMachine = new PeerConnectionStateMachine(logger, eventBus, this, networkMessageDecoder, networkMessageProcessorFactory, cancellationToken);
      }

      /// <inheritdoc/>
      public async Task IncomingConnectionAcceptedAsync(CancellationToken cancellation = default(CancellationToken))
      {
         try
         {
            await this.connectionStateMachine.AcceptIncomingConnectionAsync().ConfigureAwait(false);
         }
         catch (Exception ex)
         {
            this.logger.LogCritical(ex, "Unexpected error.");
            this.connectionStateMachine.ForceDisconnection();
         }
      }

      public ValueTask WriteAsync(INetworkMessage message, CancellationToken cancellationToken)
      {
         throw new NotImplementedException();
      }

      public ValueTask WriteManyAsync(IEnumerable<INetworkMessage> messages, CancellationToken cancellationToken)
      {
         throw new NotImplementedException();
      }
   }
}

﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Events;

namespace MithrilShards.Network.Bedrock
{
   public class BedrockForgeConnectivity : IForgeClientConnectivity
   {
      private readonly ILogger<BedrockNetworkShard> _logger;
      private readonly IEventBus _eventBus;
      private readonly MithrilForgeClientConnectionHandler _clientConnectionHandler;
      private readonly ForgeConnectivitySettings _settings;
      private readonly Client _client = null!;

      public BedrockForgeConnectivity(ILogger<BedrockNetworkShard> logger,
                                IEventBus eventBus,
                                IOptions<ForgeConnectivitySettings> settings,
                                MithrilForgeClientConnectionHandler clientConnectionHandler,
                                ClientBuilder clientBuilder)
      {
         _logger = logger;
         _eventBus = eventBus;
         _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
         _clientConnectionHandler = clientConnectionHandler;
         _client = clientBuilder
            .UseSockets()
            .UseConnectionLogging()
            .Build();
      }

      public async ValueTask AttemptConnectionAsync(OutgoingConnectionEndPoint remoteEndPoint, CancellationToken cancellation)
      {
         using IDisposable logScope = _logger.BeginScope("Outbound connection to {RemoteEndPoint}", remoteEndPoint);
         _eventBus.Publish(new PeerConnectionAttempt(remoteEndPoint.EndPoint.AsIPEndPoint()));

         bool connectionEstablished = false;

         try
         {
            await using ConnectionContext connection = await _client.ConnectAsync(remoteEndPoint.EndPoint).ConfigureAwait(false);
            // we store the RemoteEndPoint class as a feature of the connection so we can then copy it into the PeerContext in the ClientConnectionHandler.OnConnectedAsync
            connection.Features.Set(remoteEndPoint);
            connectionEstablished = true;

            await _clientConnectionHandler.OnConnectedAsync(connection).ConfigureAwait(false);
         }
         catch (OperationCanceledException)
         {
            _logger.LogDebug("Connection to {RemoteEndPoint} canceled", remoteEndPoint.EndPoint);
            _eventBus.Publish(new PeerConnectionAttemptFailed(remoteEndPoint.EndPoint.AsIPEndPoint(), "Operation canceled."));
         }
         catch (Exception ex)
         {
            if (!connectionEstablished)
            {
               _eventBus.Publish(new PeerConnectionAttemptFailed(remoteEndPoint.EndPoint.AsIPEndPoint(), ex.Message));
            }
            else
            {
               _logger.LogError(ex, "AttemptConnectionAsync failed");
               //throw;
            }
         }
      }
   }
}

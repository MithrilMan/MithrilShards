using System;
using System.Net.Sockets;
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

namespace MithrilShards.Network.Bedrock;

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
      await _eventBus.PublishAsync(new PeerConnectionAttempt(remoteEndPoint.EndPoint.AsIPEndPoint()), cancellation).ConfigureAwait(false);

      bool connectionEstablished = false;

      try
      {
         ConnectionContext connection = await _client.ConnectAsync(remoteEndPoint.EndPoint, cancellation).ConfigureAwait(false);

         // will dispose peerContext when out of scope, see https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync#using-async-disposable
         await using var connectionScope = connection.ConfigureAwait(false);

         // we store the RemoteEndPoint class as a feature of the connection so we can then copy it into the PeerContext in the ClientConnectionHandler.OnConnectedAsync
         connection.Features.Set(remoteEndPoint);
         connectionEstablished = true;

         await _clientConnectionHandler.OnConnectedAsync(connection).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
         _logger.LogDebug("Connection to {RemoteEndPoint} canceled", remoteEndPoint.EndPoint);
         await _eventBus.PublishAsync(new PeerConnectionAttemptFailed(remoteEndPoint.EndPoint.AsIPEndPoint(), "Operation canceled."), cancellation).ConfigureAwait(false);
      }
      catch (Exception ex) when (!connectionEstablished)
      {
         _logger.LogDebug("Connection to {RemoteEndPoint} failed: {Reason}", remoteEndPoint.EndPoint, ex.Message);
         await _eventBus.PublishAsync(new PeerConnectionAttemptFailed(remoteEndPoint.EndPoint.AsIPEndPoint(), ex.Message), cancellation).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
         _logger.LogDebug(ex, "AttemptConnectionAsync failed");
      }
   }
}

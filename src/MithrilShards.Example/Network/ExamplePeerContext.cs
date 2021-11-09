using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Example.Network;

public class ExamplePeerContext : PeerContext
{

   /// <summary>
   /// The peer time offset.
   /// </summary>
   public TimeSpan TimeOffset { get; set; } = TimeSpan.Zero;

   /// <summary>
   /// Peer permissions.
   /// </summary>
   public ExamplePeerPermissions Permissions { get; set; } = new ExamplePeerPermissions();

   /// <summary>
   /// Gets or sets my extra information.
   /// This information is set only for outgoing connections
   /// </summary>
   public string? MyExtraInformation { get; set; } = default;

   public ExamplePeerContext(ILogger logger,
                             IEventBus eventBus,
                             PeerConnectionDirection direction,
                             string peerId,
                             EndPoint localEndPoint,
                             EndPoint publicEndPoint,
                             EndPoint remoteEndPoint,
                             INetworkMessageWriter messageWriter)
      : base(logger, eventBus, direction, peerId, localEndPoint, publicEndPoint, remoteEndPoint, messageWriter) { }

   public override void AttachNetworkMessageProcessor(INetworkMessageProcessor messageProcessor)
   {
      base.AttachNetworkMessageProcessor(messageProcessor);
   }

   public async ValueTask OnHandshakeCompleted(Protocol.Messages.VersionMessage peerVersion)
   {
      UserAgent = peerVersion.UserAgent;
      IsConnected = true;
      await eventBus.PublishAsync(new PeerHandshaked(this)).ConfigureAwait(false);
   }
}

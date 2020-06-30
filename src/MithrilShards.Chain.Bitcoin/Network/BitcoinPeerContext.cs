using System;
using System.Net;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Network
{
   public class BitcoinPeerContext : PeerContext
   {

      /// <summary>
      /// The peer time offset.
      /// </summary>
      public TimeSpan TimeOffset { get; set; } = TimeSpan.Zero;

      /// <summary>
      /// Peer permissions.
      /// </summary>
      public BitcoinPeerPermissions Permissions { get; set; } = new BitcoinPeerPermissions();

      public BitcoinPeerContext(ILogger logger,
                                IEventBus eventBus,
                                PeerConnectionDirection direction,
                                string peerId,
                                EndPoint localEndPoint,
                                EndPoint publicEndPoint,
                                EndPoint remoteEndPoint,
                                INetworkMessageWriter messageWriter)
         : base(logger, eventBus, direction, peerId, localEndPoint, publicEndPoint, remoteEndPoint, messageWriter)
      {
      }

      public override void AttachNetworkMessageProcessor(INetworkMessageProcessor messageProcessor)
      {
         base.AttachNetworkMessageProcessor(messageProcessor);
      }

      public void OnHandshakeCompleted()
      {
         this.IsConnected = true;
         this.eventBus.Publish(new PeerHandshaked(this));
      }

      public void Disconnect(string reason)
      {
         this.IsConnected = false;
         this.eventBus.Publish(new PeerDisconnectionRequired(this.RemoteEndPoint, reason));
      }
   }
}

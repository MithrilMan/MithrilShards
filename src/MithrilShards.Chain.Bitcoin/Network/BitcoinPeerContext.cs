using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Network;

public class BitcoinPeerContext : PeerContext
{

   /// <summary>
   /// The peer time offset.
   /// </summary>
   public TimeSpan TimeOffset { get; set; } = TimeSpan.Zero;

   /// <summary>
   /// Whether this peer can give us witnesses. (fHaveWitness)
   /// </summary>
   public bool CanServeWitness { get; internal set; } = false;

   /// <summary>
   /// Whether the peer is a limited node (isn't a full node and has only a limited amount of blocks to serve).
   /// </summary>
   public bool IsLimitedNode { get; internal set; } = false;

   /// <summary>
   /// Whether this peer is a client.
   /// A Client is a node not relaying blocks and tx and not serving (parts) of the historical blockchain as "clients".
   /// </summary>
   public bool IsClient { get; internal set; } = false;

   /// <summary>
   /// Peer permissions.
   /// </summary>
   public BitcoinPeerPermissions Permissions { get; set; } = new BitcoinPeerPermissions();

   /// <summary>
   /// Gets or sets a value indicating whether the remote peer supports addrv2 (BIP155).
   /// This is typically set when a 'sendaddrv2' message is received from the peer.
   /// </summary>
   public bool SupportsAddrv2 { get; set; } = false;

   /// <summary>
   /// Gets or sets a value indicating whether the remote peer prefers wtxid-based transaction relay (BIP339).
   /// This is set when a 'wtxidrelay' message is received from the peer.
   /// Our node also sends 'wtxidrelay' to indicate its own support.
   /// </summary>
   public bool SupportsWtxidRelay { get; set; } = false;

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

   public async ValueTask OnHandshakeCompleted(Protocol.Messages.VersionMessage peerVersion)
   {
      UserAgent = peerVersion.UserAgent;
      IsConnected = true;
      await eventBus.PublishAsync(new PeerHandshaked(this)).ConfigureAwait(false);
   }
}

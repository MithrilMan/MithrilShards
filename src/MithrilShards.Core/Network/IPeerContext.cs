using System;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Core.Network;

public interface IPeerContext : IAsyncDisposable
{
   /// <summary>
   /// Gets the direction of the peer connection.
   /// </summary>
   PeerConnectionDirection Direction { get; }

   /// <summary>
   /// Gets the peer identifier.
   /// </summary>
   string PeerId { get; }

   /// <summary>
   /// Gets the local peer end point.
   /// </summary>
   IPEndPoint LocalEndPoint { get; }

   /// <summary>External IP address and port number used to access the node from external network.</summary>
   /// <remarks>Used to announce to external peers the address they connect to in order to reach our Forge server.</remarks>
   IPEndPoint PublicEndPoint { get; }

   /// <summary>
   /// Gets the remote peer end point.
   /// </summary>
   IPEndPoint RemoteEndPoint { get; }

   /// <summary>
   /// Gets the user agent.
   /// </summary>
   public string? UserAgent { get; }

   PeerMetrics Metrics { get; }

   /// <summary>
   /// Gets the version peers agrees to use when their respective version doesn't match.
   /// It should be the lower common version both parties implements.
   /// </summary>
   /// <value>
   /// The negotiated protocol version.
   /// </value>
   INegotiatedProtocolVersion NegotiatedProtocolVersion { get; }

   /// <summary>
   /// Generic container to exchange content between different components that share IPeerContext.
   /// </summary>
   IFeatureCollection Features { get; }

   /// <summary>
   /// Gets the message writer used to send a message to the peer.
   /// </summary>
   /// <returns></returns>
   INetworkMessageWriter GetMessageWriter();


   /// <summary>
   /// Attaches the network message processor to the peer context.
   /// </summary>
   /// <param name="messageProcessor">The message processor to attach.</param>
   void AttachNetworkMessageProcessor(INetworkMessageProcessor messageProcessor);

   /// <summary>
   /// Gets the connection cancellation token source in order to trigger a manual disconnection.
   /// </summary>
   CancellationTokenSource ConnectionCancellationTokenSource { get; }

   /// <summary>
   /// Gets or sets a value indicating whether a connection has been established with the peer.
   /// This flag is set if the peer already passed handshake (where expected by the protocol).
   /// </summary>
   public bool IsConnected { get; }


   /// <summary>
   /// Disconnects the peer for the specified reason.
   /// </summary>
   /// <param name="reason">The disconnection reason.</param>
   void Disconnect(string reason);
}

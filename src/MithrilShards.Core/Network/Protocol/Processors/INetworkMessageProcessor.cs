using System;
using System.Threading.Tasks;

namespace MithrilShards.Core.Network.Protocol.Processors
{
   /// <summary>
   /// Interfaces that define a processor that can handle messages received by a connected peer.
   /// </summary>
   public interface INetworkMessageProcessor : IDisposable
   {
      /// <summary>
      /// Gets a value indicating whether this <see cref="INetworkMessageProcessor"/> is enabled.
      /// Disabled processors aren't registered and so can't handle any message the remote peer is sending to us.
      /// </summary>
      /// <value>
      ///   <c>true</c> if enabled; otherwise, <c>false</c>.
      /// </value>
      bool Enabled { get; }

      /// <summary>
      /// Gets a value indicating whether this instance can receive messages.
      /// A processor may temporary disable the message handling.
      /// An example is when a processor has only to process messages when it's in a specific state (e.g. handshaked), so it can
      /// set this property to false, end enable it only after the peer passes to the handshaked state.
      /// </summary>
      /// <value>
      ///   <c>true</c> if this instance can receive messages; otherwise, <c>false</c>.
      /// </value>
      bool CanReceiveMessages { get; }

      /// <summary>
      /// Called when a new connection to a peer has been established and registered <see cref="INetworkMessageProcessor"/> are being attached to its <see cref="IPeerContext"/>.
      /// </summary>
      /// <param name="peerContext">The peer context this processor has been attached to.</param>
      ValueTask AttachAsync(IPeerContext peerContext);
   }
}
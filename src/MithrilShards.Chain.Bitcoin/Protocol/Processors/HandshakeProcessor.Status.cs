using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Events;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class HandshakeProcessor
   {
      internal class HandshakeProcessorStatus
      {
         private readonly HandshakeProcessor processor;
         internal bool IsVersionSent { get; private set; } = false;

         /// <summary>
         /// Gets the version payload received from the peer.
         /// </summary>
         internal VersionMessage? PeerVersion { get; private set; } = null;

         public bool IsHandShaked { get; private set; } = false;

         internal bool VersionAckReceived { get; private set; } = false;


         /// <summary>
         /// Whether the peer is a limited node (isn't a full node and has only a limited amount of blocks to serve).
         /// </summary>
         public bool IsLimitedNode { get; internal set; } = false;

         /// <summary>
         /// Whether this peer is a client.
         /// A Client is a node not relaying blocks and tx and not serving (parts) of the historical blockchain as "clients".
         /// </summary>
         public bool IsClient { get; internal set; } = false;

         public HandshakeProcessorStatus(HandshakeProcessor processor)
         {
            this.processor = processor;
         }

         internal void VersionSent()
         {
            this.IsVersionSent = true;
         }

         internal async ValueTask VersionReceivedAsync(VersionMessage version)
         {
            this.PeerVersion = version;
            this.processor.PeerContext.NegotiatedProtocolVersion.Version
               = Math.Min(this.PeerVersion.Version, this.processor.nodeImplementation.ImplementationVersion);

            await this.OnHandshakeStatusUpdatedAsync().ConfigureAwait(false);
         }

         internal async ValueTask VerAckReceivedAsync()
         {
            this.VersionAckReceived = true;
            await this.OnHandshakeStatusUpdatedAsync().ConfigureAwait(false);
         }

         private ValueTask OnHandshakeStatusUpdatedAsync()
         {
            if (!this.VersionAckReceived)
            {
               this.processor.logger.LogDebug("Waiting verack...");
               return default;
            }

            if (this.PeerVersion == null)
            {
               this.processor.logger.LogDebug("Waiting version message...");
               return default;
            }

            // if we reach this point, peer completed the handshake, yay!
            this.IsHandShaked = true;
            this.processor.logger.LogDebug("Handshake successful");

            this.processor.PeerContext.OnHandshakeCompleted(this.PeerVersion);

            return default;
         }
      }
   }
}

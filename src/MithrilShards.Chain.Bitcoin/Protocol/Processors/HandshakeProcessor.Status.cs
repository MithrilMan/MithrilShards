using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class HandshakeProcessor
   {
      internal class HandshakeProcessorStatus
      {
         private readonly HandshakeProcessor _processor;

         internal bool IsVersionSent { get; private set; } = false;

         /// <summary>
         /// Gets the version payload received from the peer.
         /// </summary>
         internal VersionMessage? PeerVersion { get; private set; } = null;

         public bool IsHandShaked { get; private set; } = false;

         internal bool VersionAckReceived { get; private set; } = false;

         public HandshakeProcessorStatus(HandshakeProcessor processor)
         {
            this._processor = processor;
         }

         internal void VersionSent()
         {
            this.IsVersionSent = true;
         }

         internal async ValueTask VersionReceivedAsync(VersionMessage version)
         {
            this.PeerVersion = version;
            this._processor.PeerContext.NegotiatedProtocolVersion.Version
               = Math.Min(this.PeerVersion.Version, this._processor._nodeImplementation.ImplementationVersion);

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
               this._processor.logger.LogDebug("Waiting verack...");
               return default;
            }

            if (this.PeerVersion == null)
            {
               this._processor.logger.LogDebug("Waiting version message...");
               return default;
            }

            // if we reach this point, peer completed the handshake, yay!
            this.IsHandShaked = true;
            this._processor.logger.LogDebug("Handshake successful");

            this._processor.PeerContext.OnHandshakeCompleted(this.PeerVersion);

            return default;
         }
      }
   }
}

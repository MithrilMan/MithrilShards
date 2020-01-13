using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Events;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class HandshakeProcessor
   {
      public class Status
      {
         private readonly HandshakeProcessor processor;
         internal bool VersionSent { get; private set; } = false;

         internal VersionMessage PeerVersion { get; private set; } = null;

         public bool IsHandShaked { get; private set; } = false;

         internal bool VersionAckReceived { get; private set; } = false;

         public Status(HandshakeProcessor processor)
         {
            this.processor = processor;
         }

         internal void VersionSentAsync()
         {
            this.VersionSent = true;
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

         private async ValueTask OnHandshakeStatusUpdatedAsync()
         {
            if (!this.VersionAckReceived)
            {
               this.processor.logger.LogDebug("Waiting verack...");
               return;
            }

            if (this.PeerVersion == null)
            {
               this.processor.logger.LogDebug("Waiting version message...");
               return;
            }

            // if we reach this point, peer completed the handshake, yay!
            this.IsHandShaked = true;
            this.processor.logger.LogDebug("Handshake successful");

            await this.processor.SendMessageAsync(minVersion: KnownVersion.V31402, new GetAddrMessage()).ConfigureAwait(false);

            this.processor.eventBus.Publish(new PeerHandshaked(this.processor.PeerContext));
         }
      }
   }
}

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
            _processor = processor;
         }

         internal void VersionSent()
         {
            IsVersionSent = true;
         }

         internal async ValueTask VersionReceivedAsync(VersionMessage version)
         {
            PeerVersion = version;
            _processor.PeerContext.NegotiatedProtocolVersion.Version
               = Math.Min(PeerVersion.Version, _processor._nodeImplementation.ImplementationVersion);

            await OnHandshakeStatusUpdatedAsync().ConfigureAwait(false);
         }

         internal async ValueTask VerAckReceivedAsync()
         {
            VersionAckReceived = true;
            await OnHandshakeStatusUpdatedAsync().ConfigureAwait(false);
         }

         private ValueTask OnHandshakeStatusUpdatedAsync()
         {
            if (!VersionAckReceived)
            {
               _processor.logger.LogDebug("Waiting verack...");
               return default;
            }

            if (PeerVersion == null)
            {
               _processor.logger.LogDebug("Waiting version message...");
               return default;
            }

            // if we reach this point, peer completed the handshake, yay!
            IsHandShaked = true;
            _processor.logger.LogDebug("Handshake successful");

            _processor.PeerContext.OnHandshakeCompleted(PeerVersion);

            return default;
         }
      }
   }
}

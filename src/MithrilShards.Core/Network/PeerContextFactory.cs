using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Server;

namespace MithrilShards.Core.Network {
   public class PeerContextFactory<TPeerContext> : IPeerContextFactory where TPeerContext : IPeerContext {
      readonly ILogger<PeerContextFactory<TPeerContext>> logger;
      readonly ForgeServerSettings serverSettings;

      public PeerContextFactory(ILogger<PeerContextFactory<TPeerContext>> logger, IOptions<ForgeServerSettings> serverSettings) {
         this.logger = logger;
         this.serverSettings = serverSettings?.Value;
      }

      public virtual IPeerContext Create(PeerConnectionDirection direction,
                          string peerId,
                          EndPoint localEndPoint,
                          EndPoint remoteEndPoint,
                          INetworkMessageWriter messageWriter) {

         var peerContext = (TPeerContext)System.Activator.CreateInstance(typeof(TPeerContext),
            direction,
            peerId,
            localEndPoint,
            this.GetPublicEndPoint(localEndPoint),
            remoteEndPoint,
            messageWriter
            );

         return peerContext;
      }

      protected EndPoint GetPublicEndPoint(EndPoint localEndPoint) {
         return this.serverSettings.Bindings
            .Where(binding => {
               if (!IPEndPoint.TryParse(binding.EndPoint, out IPEndPoint parsedEndPoint)) {
                  return false;
               }
               else {
                  return parsedEndPoint.Equals(localEndPoint) && binding.IsValidPublicEndPoint();
               }
            })
            .Select(binding => IPEndPoint.Parse(binding.PublicEndPoint))
            .FirstOrDefault();
      }
   }
}
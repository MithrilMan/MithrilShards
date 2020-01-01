using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Server;

namespace MithrilShards.Core.Network {
   public class PeerContextFactory<TPeerContext> : IPeerContextFactory where TPeerContext : IPeerContext {
      readonly ILogger<PeerContextFactory<TPeerContext>> logger;
      readonly ILoggerFactory loggerFactory;
      readonly ForgeConnectivitySettings serverSettings;

      public PeerContextFactory(ILogger<PeerContextFactory<TPeerContext>> logger, ILoggerFactory loggerFactory, IOptions<ForgeConnectivitySettings> serverSettings) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.serverSettings = serverSettings?.Value;
      }

      public virtual IPeerContext Create(PeerConnectionDirection direction,
                                         string peerId,
                                         EndPoint localEndPoint,
                                         EndPoint remoteEndPoint,
                                         INetworkMessageWriter messageWriter) {

         var peerContext = (TPeerContext)System.Activator.CreateInstance(typeof(TPeerContext),
            this.loggerFactory.CreateLogger<IPeerContext>(),
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
         return this.serverSettings.Listeners
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
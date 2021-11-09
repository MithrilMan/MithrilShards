using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Example.Network;

/// <summary>
/// This class generate an ExamplePeerContext.
/// In our scenario, outgoing connections must have additional information attached that information is inside the connection.Feature[<see cref="MithrilShards.Core.Network.Client.OutgoingConnectionEndPoint"/>]
/// </summary>
/// <seealso cref="PeerContextFactory{ExamplePeerContext}" />
public class ExamplePeerContextFactory : PeerContextFactory<ExamplePeerContext>
{
   public ExamplePeerContextFactory(ILogger<ExamplePeerContextFactory> logger, IEventBus eventBus, ILoggerFactory loggerFactory, IOptions<ForgeConnectivitySettings> serverSettings)
      : base(logger, eventBus, loggerFactory, serverSettings)
   {
   }

   public override IPeerContext CreateOutgoingPeerContext(string peerId, EndPoint localEndPoint, OutgoingConnectionEndPoint outgoingConnectionEndPoint, INetworkMessageWriter messageWriter)
   {
      //we know the returned type is correct because we specified it in our inheritance PeerContextFactory<ExamplePeerContext>
      var peerContext = (ExamplePeerContext)base.CreateOutgoingPeerContext(peerId, localEndPoint, outgoingConnectionEndPoint, messageWriter);

      /// outgoing PeerContext has a feature of type <see cref="OutgoingConnectionEndPoint"/> that we use to store additional information
      /// for peers we want to connect to (e.g. we could store a public key of the peer to start an encrypted communication.
      /// Since this information may be important to us, we decide to have an explicit property in our <see cref="ExamplePeerContext"/> so we can
      /// access that information easily in our code.
      /// Note that we set that information in our <see cref="Client.ExampleRequiredConnection"/> connector.
      string myExtraInformation = (string)peerContext.Features.Get<OutgoingConnectionEndPoint>().Items[nameof(ExampleEndPoint.MyExtraInformation)];

      peerContext.MyExtraInformation = myExtraInformation;

      return peerContext;
   }
}

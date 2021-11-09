using System.Net;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Core.Network;

public interface IPeerContextFactory
{
   IPeerContext CreateIncomingPeerContext(string peerId,
                                         EndPoint localEndPoint,
                                         EndPoint remoteEndPoint,
                                         INetworkMessageWriter messageWriter);

   IPeerContext CreateOutgoingPeerContext(string peerId,
                                         EndPoint localEndPoint,
                                         OutgoingConnectionEndPoint outgoingConnectionEndPoint,
                                         INetworkMessageWriter messageWriter);
}

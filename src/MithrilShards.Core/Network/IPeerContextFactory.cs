using System.Net;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Core.Network
{
   public interface IPeerContextFactory
   {
      IPeerContext Create(PeerConnectionDirection direction,
                          string peerId,
                          EndPoint localEndPoint,
                          EndPoint remoteEndPoint,
                          INetworkMessageWriter messageWriter);
   }
}
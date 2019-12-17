using System.Collections.Generic;

namespace MithrilShards.P2P.Network.Server {
   public interface IServerPeerFactory {
      List<IServerPeer> CreateServerInstances();
   }
}
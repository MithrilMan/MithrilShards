using System.Collections.Generic;

namespace MithrilShards.Network.Network.Server {
   public interface IServerPeerFactory {
      List<IServerPeer> CreateServerInstances();
   }
}
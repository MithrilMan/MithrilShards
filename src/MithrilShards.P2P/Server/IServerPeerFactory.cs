using System.Collections.Generic;

namespace MithrilShards.Network.Legacy.Server {
   public interface IServerPeerFactory {
      List<IServerPeer> CreateServerInstances();
   }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MithrilShards.P2P.Network.Server {
   public interface IServerPeer {
      Task ListenAsync(CancellationToken cancellation);

      void StopListening();
   }
}

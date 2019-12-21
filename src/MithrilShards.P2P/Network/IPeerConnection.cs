using MithrilShards.Core.Network;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.P2P.Network {
   public interface IPeerConnection {
      PeerConnectionDirection Direction { get; }
      PeerDisconnectionReason DisconnectReason { get; }
      Guid PeerConnectionId { get; }
      TimeSpan? TimeOffset { get; }

      Task IncomingConnectionAccepted(CancellationToken cancellation = default);
   }
}
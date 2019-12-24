using System.Net;

namespace MithrilShards.Core.Network {
   public interface IPeerContext {
      /// <summary>
      /// Gets the direction of the peer connection.
      /// </summary>
      PeerConnectionDirection Direction { get; }

      /// <summary>
      /// Gets the peer identifier.
      /// </summary>
      string PeerId { get; }


      /// <summary>
      /// Gets the local peer end point.
      /// </summary>
      IPEndPoint LocalEndPoint { get; }

      /// <summary>
      /// Gets the remote peer end point.
      /// </summary>
      IPEndPoint RemoteEndPoint { get; }
   }
}
using System;
using System.Net;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Network {
   public class BitcoinPeerContext : PeerContext {

      public TimeSpan? TimeOffset { get; set; }

      public int NegotiatedVersion { get; set; }

      public BitcoinPeerContext(PeerConnectionDirection direction,
                         string peerId,
                         EndPoint localEndPoint,
                         EndPoint publicEndPoint,
                         EndPoint remoteEndPoint,
                         INetworkMessageWriter messageWriter)
         : base(direction, peerId, localEndPoint, publicEndPoint, remoteEndPoint, messageWriter) {

      }
   }
}

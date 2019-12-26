using System;
using MithrilShards.Chain.Bitcoin.Protocol;

namespace MithrilShards.Chain.Bitcoin.Network {
   public class NegotiatedVersion {
      /// <summary>
      /// Gets the negotiated version (the minimum version known by both parties involved.
      /// </summary>
      public int Version { get; }

      public DateTimeOffset TimeOffset { get; }

      public NegotiatedVersion(int version) {
         this.Version = version;
      }
   }
}

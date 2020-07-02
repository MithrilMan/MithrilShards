using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public class HeadersToValidate
   {
      public IReadOnlyCollection<BlockHeader> Headers { get; }

      public IPeerContext Peer { get; }

      public HeadersToValidate(IReadOnlyCollection<BlockHeader> headers, IPeerContext peer)
      {
         this.Headers = headers;
         this.Peer = peer;
      }
   }
}
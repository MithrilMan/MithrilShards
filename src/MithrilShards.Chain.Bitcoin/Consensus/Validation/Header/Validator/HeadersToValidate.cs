using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{

   /// <summary>
   /// Represents a collection of headers that have to be validated.
   /// Headers must be consecutive in order to be validated.
   /// </summary>
   public class HeadersToValidate
   {
      /// <summary>
      /// The Headers that have to be validated (expected to be consecutive).
      /// </summary>
      public IReadOnlyCollection<BlockHeader> Headers { get; }

      /// <summary>
      /// The peer that's requiring the validation.
      /// Null if the request comes from other sources (e.g. validating a block fetched from disk)
      /// </summary>
      public IPeerContext? Peer { get; }

      public HeadersToValidate(IReadOnlyCollection<BlockHeader> headers, IPeerContext? peer)
      {
         this.Headers = headers;
         this.Peer = peer;
      }
   }
}
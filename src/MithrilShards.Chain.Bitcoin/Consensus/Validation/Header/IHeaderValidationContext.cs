using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   public interface IHeaderValidationContext
   {
      BlockHeader Header { get; }

      Dictionary<object, object> Items { get; }

      HeadersTree HeadersTree { get; }
   }
}
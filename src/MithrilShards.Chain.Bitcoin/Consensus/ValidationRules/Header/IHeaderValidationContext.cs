using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.ValidationRules
{
   public interface IHeaderValidationContext
   {
      BlockHeader Header { get; }

      Dictionary<object, object> Items { get; }
   }
}
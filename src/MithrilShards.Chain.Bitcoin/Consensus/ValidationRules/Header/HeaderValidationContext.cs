using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.ValidationRules
{
   public class HeaderValidationContext : IHeaderValidationContext
   {
      public BlockHeader Header { get; }

      public Dictionary<object, object> Items => new Dictionary<object, object>();

      public HeaderValidationContext(BlockHeader header)
      {
         this.Header = header;
      }
   }
}

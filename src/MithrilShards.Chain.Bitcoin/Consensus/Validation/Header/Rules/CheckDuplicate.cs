using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules
{
   public class CheckDuplicate : HeaderValidationRuleBase
   {
      public CheckDuplicate(ILogger<CheckDuplicate> logger) : base(logger) { }

      public override bool Check(IHeaderValidationContext context)
      {
         BlockHeader header = context.Header;

       
      }
   }
}

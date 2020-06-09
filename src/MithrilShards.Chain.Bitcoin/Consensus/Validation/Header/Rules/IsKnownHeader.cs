using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.Rules
{
   /// <summary>
   /// Check if the header is a known header
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Consensus.Validation.Header.HeaderValidationRuleBase" />
   public class IsKnownHeader : HeaderValidationRuleBase
   {
      public IsKnownHeader(ILogger<IsKnownHeader> logger) : base(logger) { }

      public override bool Check(IHeaderValidationContext context, ref BlockValidationState validationState)
      {
         BlockHeader header = context.Header;


         return true; //TODO implement the rule properly

         //var bits = header.Bits.ToBigInteger();
         //if (bits.CompareTo(BigInteger.Zero) <= 0 || bits.CompareTo(Pow256) >= 0)
         //   return false;

         //return GetPoWHash() <= this.Bits.ToUInt256();

         //TODO

         //bool fNegative;
         //bool fOverflow;
         //arith_uint256 bnTarget;

         //bnTarget.SetCompact(nBits, &fNegative, &fOverflow);

         //// Check range
         //if (fNegative || bnTarget == 0 || fOverflow || bnTarget > UintToArith256(params.powLimit))
         //   return false;

         //// Check proof of work matches claimed amount
         //if (UintToArith256(hash) > bnTarget)
         //   return false;

         //return true;
      }
   }
}

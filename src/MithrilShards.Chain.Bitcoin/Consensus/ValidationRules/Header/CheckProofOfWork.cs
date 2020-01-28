using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.ValidationRules.Header
{
   public class CheckProofOfWork : HeaderValidationRuleBase
   {
      public CheckProofOfWork(ILogger<CheckProofOfWork> logger) : base(logger) { }

      public override void Check(IHeaderValidationContext context)
      {
         //BlockHeader header = context.Header;
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

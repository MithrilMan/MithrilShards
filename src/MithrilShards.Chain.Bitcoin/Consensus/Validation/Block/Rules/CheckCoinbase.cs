using Microsoft.Extensions.Logging;


namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Rules
{
   public class CheckCoinbase : IBlockValidationRule
   {
      readonly ILogger<CheckCoinbase> logger;

      public CheckCoinbase(ILogger<CheckCoinbase> logger)
      {
         this.logger = logger;
      }


      public bool Check(IBlockValidationContext context, ref BlockValidationState validationState)
      {
         Protocol.Types.Transaction[] transactions = context.Block.Transactions!;

         // First transaction must be coinbase, the rest must not be
         if ((transactions.Length == 0) || !transactions[0].IsCoinBase())
         {
            validationState.Invalid(BlockValidationStateResults.Consensus, "bad-cb-missing", "first tx is not coinbase");
            return false;
         }

         for (int i = 1; i < transactions.Length; i++)
         {
            if (transactions[i].IsCoinBase())
            {
               validationState.Invalid(BlockValidationStateResults.Consensus, "bad-cb-multiple", "more than one coinbase");
               return false;
            }
         }

         return true;
      }
   }
}
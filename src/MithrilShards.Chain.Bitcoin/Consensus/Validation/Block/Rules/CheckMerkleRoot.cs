using System;
using System.Numerics;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Rules
{
   public class CheckMerkleRoot : IBlockValidationRule
   {
      readonly ILogger<CheckMerkleRoot> logger;
      readonly IMerkleRootCalculator merkleRootCalculator;

      public CheckMerkleRoot(ILogger<CheckMerkleRoot> logger, IMerkleRootCalculator merkleRootCalculator)
      {
         this.logger = logger;
         this.merkleRootCalculator = merkleRootCalculator;
      }


      public bool Check(IBlockValidationContext context, ref BlockValidationState validationState)
      {
         if (this.IsBlockMalleated(context.Block.Transactions!))
         {
            return validationState.Invalid(BlockValidationStateResults.Mutated, "bad-txns-duplicate", "duplicate transaction");
         }

         UInt256 computedMerkleRoot = merkleRootCalculator.GetBlockMerkleRoot(context.Block);
         if (context.Block.Header!.MerkleRoot != computedMerkleRoot)
         {
            return validationState.Invalid(BlockValidationStateResults.Mutated, "bad-txnmrklroot", "hashMerkleRoot mismatch");
         }

         return true;
      }

      /// <summary>
      /// Check if a block is malleated to be susceptible to CVE-2012-2459.
      /// The assumption is that transaction after the Safe Point, where the Safe Point is the Nth element that represents the closest
      /// power of the 2nd element compared to the number of transactions (non inclusive), cannot be duplicated.
      /// e.g.
      ///   if we have 9 transaction, Safe Point = 8, that's the closest, non inclusive pow of 2 => 2^3 = 8 and 8 less than transactionCount.
      ///   if we have 16 transaction Safe Point is still = 8 because 16 is a power of 2 and we have to get the closest power of 2 (non inclusive).
      ///
      /// In both previous scenarios, first 8 transactions can't be duplicated.
      ///
      /// Now to check if a block is compromised, we have to compare last transaction with preceeding, with exponential steps, up to the point we go into the Safe Point rage.
      ///
      /// example of an altered block
      /// A B C D E F G H | I L M N I L M N
      /// | is the Safe Point
      ///
      /// Algorihtm then is:
      /// - we set the exponentialStep to apply to each iteration to : 1 if transaction count is even, 2 if transaction count is odd.
      /// - we take lastElement (N)
      /// - we set the transactionIndexToCompare to last element = second last element (the other M)
      ///
      /// - until our moving index is greater than the Safe Point, we do
      ///   - we check to see if lastElement (N) matches the element at transactionIndexToCompare position. (if yes return true, means block is corrupted)
      ///   - decrement transactionIndexToCompare by expStep (basically moving to the left by an exponential amount base 2, incremented every iteration
      ///   - multiply expStep by two (exponential incrementation)
      /// </summary>
      /// <param name="transactions"></param>
      /// <returns></returns>
      private bool IsBlockMalleated(Transaction[] transactions)
      {
         const uint transactionCountBitLength = sizeof(uint) * 8;

         uint transactionsCount = (uint)transactions.Length;
         bool transactionCountIsOdd = (transactionsCount & 1) == 1;

         uint higherBitPosition = (uint)(transactionCountBitLength - (BitOperations.LeadingZeroCount(transactionsCount - 1) + 1));
         uint safePoint = ((uint)Math.Pow(2, higherBitPosition));
         uint itemsToConsider = transactionsCount - safePoint;

         Transaction lastElement = transactions[transactionsCount - 1];
         uint transactionIndexToCompare = transactionsCount - 2;
         uint expStep = transactionCountIsOdd ? 2u : 1u;

         while (expStep < itemsToConsider)
         {
            if (transactions[transactionIndexToCompare] == lastElement)
            {
               return true;
            }

            transactionIndexToCompare -= expStep;
            expStep <<= 1;
         }

         return false;
      }
   }
}
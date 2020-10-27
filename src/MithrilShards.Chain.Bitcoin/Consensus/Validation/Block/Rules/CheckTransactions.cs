using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Rules
{
   public class CheckTransactions : IBlockValidationRule
   {
      readonly ILogger<CheckTransactions> logger;
      readonly IProtocolTypeSerializer<Transaction> transactionSerializer;
      readonly IConsensusParameters consensusParameters;

      public CheckTransactions(ILogger<CheckTransactions> logger, IProtocolTypeSerializer<Transaction> transactionSerializer, IConsensusParameters consensusParameters)
      {
         this.logger = logger;
         this.transactionSerializer = transactionSerializer;
         this.consensusParameters = consensusParameters;
      }


      public bool Check(IBlockValidationContext context, ref BlockValidationState validationState)
      {
         Transaction[] transactions = context.Block.Transactions!;

         for (int i = 0; i < transactions.Length; i++)
         {
            if (!PerformCheck(transactions[i], out TransactionValidationState? transactionValidationState))
            {
               // checks performed here are context-free validation checks. The only possible failures are consensus failures.
               return validationState.Invalid(BlockValidationStateResults.Consensus, transactionValidationState.RejectReason!, transactionValidationState.DebugMessage!);
            }
         }

         return true;
      }



      private bool PerformCheck(Transaction transaction, [MaybeNullWhen(true)] out TransactionValidationState state)
      {
         state = new TransactionValidationState();

         if (transaction.Inputs!.Length == 0)
         {
            return state.Invalid(TransactionValidationStateResults.Consensus, "bad-txns-vin-empty");
         }

         if (transaction.Outputs!.Length == 0)
         {
            return state.Invalid(TransactionValidationStateResults.Consensus, "bad-txns-vout-empty");
         }

         // Size limits (this doesn't take the witness into account, as that hasn't been checked for malleability)
         int size = this.transactionSerializer.Serialize(transaction, KnownVersion.CurrentVersion, new ArrayBufferWriter<byte>(), new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, false)));
         if (size * consensusParameters.WitnessScaleFactor > consensusParameters.MaxBlockWeight)
         {
            return state.Invalid(TransactionValidationStateResults.Consensus, "bad-txns-oversize");
         }

         // Check for negative or overflow output values (see CVE-2010-5139)
         long totalOutput = 0;
         foreach (TransactionOutput output in transaction.Outputs)
         {
            if (output.Value < 0)
            {
               return state.Invalid(TransactionValidationStateResults.Consensus, "bad-txns-vout-negative");
            }

            if (output.Value > consensusParameters.MaxMoney)
            {
               return state.Invalid(TransactionValidationStateResults.Consensus, "bad-txns-vout-toolarge");
            }

            totalOutput += output.Value;
            if (totalOutput < 0 || totalOutput > consensusParameters.MaxMoney)
            {
               return state.Invalid(TransactionValidationStateResults.Consensus, "bad-txns-txouttotal-toolarge");
            }
         }


         // Check for duplicate inputs (see CVE-2018-17144)
         // While Consensus::CheckTxInputs does check if all inputs of a tx are available, and UpdateCoins marks all inputs
         // of a tx as spent, it does not check if the tx has duplicate inputs.
         // Failure to run this check will result in either a crash or an inflation bug, depending on the implementation of
         // the underlying coins database.
         HashSet<OutPoint> usedOutPoints = new HashSet<OutPoint>();
         foreach (TransactionInput input in transaction.Inputs)
         {
            if (!usedOutPoints.Add(input.PreviousOutput!))
            {
               return state.Invalid(TransactionValidationStateResults.Consensus, "bad-txns-inputs-duplicate");
            }
         }

         if (transaction.IsCoinBase())
         {
            // ensure coinbase transaction input has a signature script with proper size

            int firstInputScriptSignatureLength = transaction.Inputs[0].SignatureScript!.Length;
            if (firstInputScriptSignatureLength < 2 || firstInputScriptSignatureLength > 100)
            {
               return state.Invalid(TransactionValidationStateResults.Consensus, "bad-cb-length");
            }
         }
         else
         {
            foreach (TransactionInput input in transaction.Inputs)
            {
               if (input.PreviousOutput!.IsNull())
               {
                  return state.Invalid(TransactionValidationStateResults.Consensus, "bad-txns-prevout-null");
               }
            }
         }

         return true;
      }
   }
}
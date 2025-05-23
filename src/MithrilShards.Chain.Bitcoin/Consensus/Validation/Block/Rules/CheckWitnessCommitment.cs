using System.Buffers;
using System.Linq;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Rules;

/// <summary>
/// Validates the witness commitment in a block, as per BIP141.
/// This rule checks that if a block contains witness transactions, its coinbase transaction
/// correctly commits to the witness Merkle root.
/// </summary>
public class CheckWitnessCommitment : IBlockValidationRule
{
   private readonly ILogger<CheckWitnessCommitment> _logger;
   private readonly IConsensusParameters _consensusParameters;
   private readonly IProtocolTypeSerializer<Transaction> _transactionSerializer;
   private readonly IMerkleRootCalculator _merkleRootCalculator; // Will be needed

   // Commitment header: OP_RETURN OP_PUSHBYTES_36 0xaa21a9ed
   private static readonly byte[] WITNESS_COMMITMENT_HEADER = { 0x6a, 0x24, 0xaa, 0x21, 0xa9, 0xed };

   public CheckWitnessCommitment(ILogger<CheckWitnessCommitment> logger,
                                 IConsensusParameters consensusParameters,
                                 IProtocolTypeSerializer<Transaction> transactionSerializer,
                                 IMerkleRootCalculator merkleRootCalculator)
   {
      _logger = logger;
      _consensusParameters = consensusParameters;
      _transactionSerializer = transactionSerializer;
      _merkleRootCalculator = merkleRootCalculator;
   }

   public bool Check(IBlockValidationContext context, ref BlockValidationState validationState)
   {
      Block block = context.Block;
      bool blockContainsWitness = false;
      for (int i = 0; i < block.Transactions!.Length; i++)
      {
         if (block.Transactions[i].HasWitness())
         {
            blockContainsWitness = true;
            break;
         }
      }

      UInt256? witnessCommitmentFromCoinbase = FindWitnessCommitmentInCoinbase(block.Transactions[0]);

      if (blockContainsWitness)
      {
         if (witnessCommitmentFromCoinbase == null)
         {
            // BIP141: "If any transaction in the block has a witness, the coinbase transaction MUST include a witness commitment."
            return validationState.Invalid(BlockValidationStateResults.Consensus, "bad-witness-missing", "Block contains witness data but coinbase missing witness commitment.");
         }
      }
      else // No transactions in the block have witness data
      {
         if (witnessCommitmentFromCoinbase != null)
         {
            // BIP141: "If no transaction in the block has a witness, the coinbase transaction MUST NOT include a witness commitment."
            // However, Bitcoin Core's current policy is more relaxed: a commitment is allowed even if no tx has witness,
            // but if present, it must be valid. Let's follow a stricter interpretation for now or make it configurable.
            // For now, let's assume if it's present, it must be valid. If not present, it's fine.
            // If it IS present, it will be validated by the logic below. So, no specific error here if block has no witness but commitment is present.
         }
      }

      // If a commitment is present in the coinbase, it must be valid.
      if (witnessCommitmentFromCoinbase != null)
      {
         UInt256 calculatedWitnessCommitment = CalculateWitnessCommitment(block);
         if (!witnessCommitmentFromCoinbase.Equals(calculatedWitnessCommitment))
         {
            _logger.LogDebug("Witness commitment mismatch. Coinbase: {CoinbaseCommitment}, Calculated: {CalculatedCommitment}",
                             witnessCommitmentFromCoinbase.ToString(), calculatedWitnessCommitment.ToString());
            return validationState.Invalid(BlockValidationStateResults.Consensus, "bad-witness-merkle-match", "Witness commitment hash mismatch in coinbase.");
         }
      }
      // If blockContainsWitness is true AND witnessCommitmentFromCoinbase was null, we've already failed above.
      // If blockContainsWitness is false AND witnessCommitmentFromCoinbase was null, it's valid.

      return true;
   }

using System.Diagnostics.CodeAnalysis; // For MaybeNullWhen

// ... (rest of using statements remain the same)

// ... (class definition and constructor remain the same)

   public bool Check(IBlockValidationContext context, ref BlockValidationState validationState)
   {
      Block block = context.Block;
      bool blockContainsWitness = false;
      for (int i = 0; i < block.Transactions!.Length; i++)
      {
         if (block.Transactions[i].HasWitness())
         {
            blockContainsWitness = true;
            break;
         }
      }

      if (!TryFindWitnessCommitmentInCoinbase(block.Transactions[0], out UInt256? witnessCommitmentFromCoinbase, ref validationState))
      {
         // validationState is already set by TryFindWitnessCommitmentInCoinbase if it returned false (e.g. multiple commitments).
         // No need to set it again here, just return false.
         return false;
      }

      if (blockContainsWitness)
      {
         if (witnessCommitmentFromCoinbase == null)
         {
            // BIP141: "If any transaction in the block has a witness, the coinbase transaction MUST include a witness commitment."
            return validationState.Invalid(BlockValidationStateResults.Consensus, "bad-witness-missing", "Block contains witness data but coinbase missing witness commitment.");
         }

         // If commitment is present and block has witnesses, it must be valid.
         UInt256 calculatedWitnessCommitment = CalculateWitnessCommitment(block);
         if (!witnessCommitmentFromCoinbase.Equals(calculatedWitnessCommitment))
         {
            _logger.LogDebug("Witness commitment mismatch. Coinbase: {CoinbaseCommitment}, Calculated: {CalculatedCommitment}",
                             witnessCommitmentFromCoinbase.ToString(), calculatedWitnessCommitment.ToString());
            return validationState.Invalid(BlockValidationStateResults.Consensus, "bad-witness-merkle-match", "Witness commitment hash mismatch in coinbase.");
         }
      }
      else // No transactions in the block have witness data
      {
         // BIP141: "If no transaction in the block has a witness, the coinbase transaction MUST NOT include a witness commitment."
         // This is now strictly enforced by Bitcoin Core.
         if (witnessCommitmentFromCoinbase != null)
         {
            return validationState.Invalid(BlockValidationStateResults.Consensus, "bad-witness-unnecessary", "Coinbase contains witness commitment but no transactions in block have witness data.");
         }
         // If no witness in block and no commitment in coinbase, it's valid. Nothing more to check.
      }

      return true;
   }

   private bool TryFindWitnessCommitmentInCoinbase(Transaction coinbaseTx, [MaybeNullWhen(false)] out UInt256 commitment, ref BlockValidationState validationState)
   {
      commitment = null; // Initialize out parameter
      if (!coinbaseTx.IsCoinBase())
      {
         // This should be caught by earlier validation rules (e.g. CheckCoinbase ensuring block.Transactions[0] is coinbase)
         // but as a defensive measure within this specific logic:
         _logger.LogError("TryFindWitnessCommitmentInCoinbase called with a non-coinbase transaction. This indicates a flaw in the validation pipeline order or logic.");
         // This is a critical internal error, not a typical consensus failure of the block itself.
         // However, to ensure safety, we treat it as an invalid state for this rule.
         validationState.Invalid(BlockValidationStateResults.Consensus, "internal-error-commitment-check-on-non-coinbase", "Internal: Attempted to find witness commitment in a non-coinbase transaction.");
         return false;
      }

      UInt256? foundCommitment = null;
      bool multipleCommitments = false;

      foreach (TransactionOutput output in coinbaseTx.Outputs!)
      {
         if (output.ScriptPubKey != null && output.ScriptPubKey.Length >= 38 &&
             output.ScriptPubKey.Take(WITNESS_COMMITMENT_HEADER.Length).SequenceEqual(WITNESS_COMMITMENT_HEADER))
         {
            if (foundCommitment != null)
            {
               // Found more than one commitment.
               multipleCommitments = true;
               break; // No need to look further.
            }
            if (output.ScriptPubKey.Length == 38) // OP_RETURN OP_PUSHBYTES_36 <36_bytes>
            {
               foundCommitment = new UInt256(output.ScriptPubKey.Skip(WITNESS_COMMITMENT_HEADER.Length).Take(32).ToArray());
            }
            else
            {
               // Invalid length for the commitment scriptPubKey
               _logger.LogDebug("Found witness commitment header but scriptPubKey has invalid length: {Length}", output.ScriptPubKey.Length);
               // This should also be a consensus failure.
               validationState.Invalid(BlockValidationStateResults.Consensus, "bad-witness-commitment-invalid-length", "Witness commitment scriptPubKey has invalid length.");
               return false;
            }
         }
      }

      if (multipleCommitments)
      {
         _logger.LogDebug("Multiple witness commitments found in coinbase.");
         validationState.Invalid(BlockValidationStateResults.Consensus, "bad-cb-multiple-commitment", "Coinbase contains multiple witness commitments.");
         return false;
      }

      commitment = foundCommitment;
      return true; // True, commitment might be null (if not found) or contain the value.
   }

   private UInt256 CalculateWitnessCommitment(Block block)
   {
      var witnessHashes = new List<UInt256>(block.Transactions!.Length);
      // The first transaction is coinbase. Its wtxid for the witness Merkle tree is all zeros.
      // Also, its own witness stack must be empty (this should be checked by another rule, e.g. CheckCoinbase or tx validation).
      witnessHashes.Add(UInt256.Zero);

      for (int i = 1; i < block.Transactions.Length; i++) // Start from the second transaction
      {
         Transaction tx = block.Transactions[i];
         if (tx.WitnessHash != null)
         {
            witnessHashes.Add(tx.WitnessHash);
         }
         else
         {
            var buffer = new ArrayBufferWriter<byte>();
            // Ensure SERIALIZE_WITNESS is true for wtxid calculation
            _transactionSerializer.Serialize(tx, KnownVersion.CurrentVersion, buffer, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));
            witnessHashes.Add(Hashing.Hash256(buffer.WrittenSpan));
         }
      }

      UInt256 witnessMerkleRoot = _merkleRootCalculator.ComputeMerkleRoot(witnessHashes);
      var witnessReservedValue = UInt256.Zero; // 32 bytes of zeros

      byte[] combined = new byte[64];
      witnessMerkleRoot.GetBytes().CopyTo(combined, 0);
      witnessReservedValue.GetBytes().CopyTo(combined, 32);

      return Hashing.Hash256(combined);
   }
}

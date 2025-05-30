using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Transaction;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Rules;

/// <summary>
/// Validates the scripts of all transactions in a block.
/// This rule is context-dependent and requires access to the UTXO set.
/// It should run after context-free transaction checks and after the UTXO view for the block is prepared.
/// </summary>
public class ValidateTransactionScriptsRule : IBlockValidationRule
{
   private readonly ILogger<ValidateTransactionScriptsRule> _logger;
   private readonly TransactionScriptValidator _transactionScriptValidator;
   private readonly IConsensusParameters _consensusParameters;

   public ValidateTransactionScriptsRule(ILogger<ValidateTransactionScriptsRule> logger,
                                         TransactionScriptValidator transactionScriptValidator,
                                         IConsensusParameters consensusParameters)
   {
      _logger = logger;
      _transactionScriptValidator = transactionScriptValidator;
      _consensusParameters = consensusParameters;
   }

   public bool Check(IBlockValidationContext context, ref BlockValidationState validationState)
   {
      Protocol.Types.Block block = context.Block;
      ICoinsView coinsView = context.CoinsView; // This is expected to be a CoinsViewCache

      // Determine ScriptSettings based on block context (e.g., height, MTP)
      // For Taproot, this means checking if Taproot is active for this block.
      // This logic might be more complex in a real node (e.g. using IChainState and block header)
      // Determine ScriptSettings based on block context (e.g., height, MTP)
      // The block's height for activation checks should be the height it would have if accepted.
      // context.ChainTip.Height is the height of the *parent* block. So, new block height is parent_height + 1.
      int blockHeight = (context.ChainTip?.Height ?? -1) + 1; // if ChainTip is null (genesis), height is 0.
      if (block.Header.PreviousBlockHash == null || block.Header.PreviousBlockHash.IsNull) // Genesis block
      {
         blockHeight = 0;
      }

      // P2SH activation is typically at a specific block height (e.g., _consensusParameters.BIP16Height)
      // For most networks now, P2SH is always active.
      bool isP2shEnabled = true; // Assuming P2SH is active.

      bool isSegWitActive = _consensusParameters.SegwitHeight >= 0 && blockHeight >= _consensusParameters.SegwitHeight;
      bool isTaprootActive = _consensusParameters.TaprootActivationHeight >= 0 && blockHeight >= _consensusParameters.TaprootActivationHeight;

      var scriptSettings = new ScriptSettings
      {
         IsP2shEnabled = isP2shEnabled,
         IsWitnessEnabled = isSegWitActive,
         IsTaprootEnabled = isTaprootActive,
         ScriptVerifyFlags = GetScriptVerifyFlags(isTaprootActive, isSegWitActive, isP2shEnabled)
      };

      // Iterate through all transactions in the block (excluding coinbase)
      for (int i = 1; i < block.Transactions!.Length; i++)
      {
         Transaction tx = block.Transactions[i];

         // Fetch spentOutputs for the current transaction tx
         var spentOutputs = new TransactionOutput[tx.Inputs!.Length];
         for (int j = 0; j < tx.Inputs.Length; j++)
         {
            TransactionInput txInput = tx.Inputs[j];
            OutPoint prevOut = txInput.PreviousOutput!;

            if (!coinsView.TryGetOutput(prevOut, out TransactionOutput? spentOutput))
            {
               // This check might be redundant if a prior rule (like CheckInputs) already ensures inputs are available.
               // However, good to have a safeguard or if this rule is run more independently.
               _logger.LogDebug("Input {InputHash}:{InputN} for tx {TransactionHash} not found in UTXO set.", prevOut.Hash, prevOut.Index, tx.Hash);
               return validationState.Invalid(BlockValidationStateResults.MissingInputs, // Assuming this result exists
                                              $"bad-txns-inputs-missingorspent",
                                              $"Input {prevOut.Hash}:{prevOut.Index} for tx {tx.Hash} not found");
            }
            spentOutputs[j] = spentOutput!;
         }

         // Iterate through inputs of the current transaction and validate scripts
         for (int j = 0; j < tx.Inputs.Length; j++)
         {
            if (!_transactionScriptValidator.VerifyScript(tx, j, spentOutputs, scriptSettings, out ScriptError scriptError))
            {
               _logger.LogDebug("Script validation failed for tx {TransactionHash} input {InputIndex}. Error: {ScriptError}", tx.Hash, j, scriptError);
               return validationState.Invalid(BlockValidationStateResults.MandatoryScriptVerifyFlagFailed, // Assuming this result exists
                                              $"script-verify-failed",
                                              $"Script failed for input {j} of tx {tx.Hash} with error {scriptError}");
            }
         }
      }

      return true; // All scripts in all (non-coinbase) transactions validated successfully
   }

   private ScriptFlags GetScriptVerifyFlags(bool isTaprootActive, bool isSegWitActive, bool isP2shActive)
   {
      ScriptFlags flags = ScriptFlags.None;

      if (isP2shActive)
      {
         flags |= ScriptFlags.P2SH;
      }

      // Standard script verification flags that are generally always on after their activation.
      // For simplicity, we tie most of these to SegWit activation, as many became standard around then.
      if (isSegWitActive) // SegWit activation implies these flags are generally active.
      {
         flags |= ScriptFlags.StrictEncoding;      // SCRIPT_VERIFY_DERSIG, SCRIPT_VERIFY_LOW_S, SCRIPT_VERIFY_STRICTENC
         flags |= ScriptFlags.MinimalData;          // SCRIPT_VERIFY_MINIMALDATA
         flags |= ScriptFlags.NullFail;             // SCRIPT_VERIFY_NULLFAIL
         flags |= ScriptFlags.CheckLockTimeVerify;  // SCRIPT_VERIFY_CHECKLOCKTIMEVERIFY
         flags |= ScriptFlags.CheckSequenceVerify;  // SCRIPT_VERIFY_CHECKSEQUENCEVERIFY
         flags |= ScriptFlags.DiscourageUpgradableWitness; // SCRIPT_VERIFY_DISCOURAGE_UPGRADABLE_WITNESS_PROGRAM
      }

      if (isSegWitActive) // SegWit itself (includes P2WPKH, P2WSH specific rules)
      {
         flags |= ScriptFlags.Witness;    // Enables general witness validation.
         flags |= ScriptFlags.WitnessV0;  // Specific to SegWit Version 0 (P2WPKH, P2WSH) validation rules.
         flags |= ScriptFlags.CleanStack; // SCRIPT_VERIFY_CLEANSTACK (also for P2SH if Witness is enabled)
      }

      if (isTaprootActive)
      {
         flags |= ScriptFlags.Taproot; // Enables Taproot-specific validation (BIP341/BIP342)
                                       // Taproot implies Witness and CleanStack.
                                       // NullFail is also critical and enforced by Taproot rules directly.
         flags |= ScriptFlags.NullFail; // Explicitly ensure NullFail is active if Taproot is.
                                       // StrictEncoding, MinimalData also apply and are typically enabled with SegWit.
      }
      // Note: Some flags might be redundant if one implies another (e.g. Taproot implies Witness).
      // The ScriptInterpreter should handle the combination of flags correctly.
      return flags;
   }
}

using System.Collections.Generic;
using System.Linq;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Script;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
// using MithrilShards.Core.DataTypes; // Not strictly needed here if amounts are long
using MithrilShards.Chain.Bitcoin.Crypto; // For HashUtils and SchnorrVerifierBouncyCastle

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Transaction
{
   public class TransactionScriptValidator
   {
      private readonly ScriptInterpreter _interpreter;
      // private readonly ILogger<TransactionScriptValidator> _logger;

      public TransactionScriptValidator(ScriptInterpreter interpreter) // ILogger<TransactionScriptValidator> logger)
      {
         _interpreter = interpreter;
         // _logger = logger;
      }

      /// <summary>
      /// Verifies the script of a given transaction input.
      /// </summary>
      /// <param name="txSpending">The transaction spending the outputs.</param>
      /// <param name="inputIndex">The index of the input in txSpending being validated.</param>
      /// <param name="spentOutputs">An array of all UTXOs being spent by txSpending. The order must match the inputs in txSpending.</param>
      /// <param name="settings">Script validation settings/flags.</param>
      /// <param name="error">Output parameter for any script error encountered.</param>
      /// <returns>True if the script validates, false otherwise.</returns>
      public bool VerifyScript(Transaction txSpending,
                               int inputIndex,
                               TransactionOutput[] spentOutputs, // Changed for Taproot: all spent outputs are needed
                               ScriptSettings settings,
                               out ScriptError error)
      {
         error = ScriptError.OK;

         if (inputIndex < 0 || inputIndex >= txSpending.Inputs!.Length)
         {
            error = ScriptError.UNKNOWN_ERROR; // Or more specific: INVALID_INPUT_INDEX
            return false;
         }
         if (spentOutputs == null || spentOutputs.Length != txSpending.Inputs.Length)
         {
            error = ScriptError.UNKNOWN_ERROR; // Or more specific: SPENT_OUTPUTS_MISMATCH
            return false;
         }

         TransactionInput txInput = txSpending.Inputs[inputIndex];
         TransactionOutput utxo = spentOutputs[inputIndex]; // The specific UTXO for the current input
         long utxoAmount = utxo.Value;
         byte[] utxoScriptPubKeyBytes = utxo.ScriptPubKey!;


         byte[] scriptSigBytes = txInput.SignatureScript ?? System.Array.Empty<byte>();
         if (!ScriptParser.TryParse(scriptSigBytes, out IList<ScriptElement> parsedScriptSigElements, out error))
         {
            return false;
         }

         if (!ScriptParser.TryParse(utxoScriptPubKeyBytes, out IList<ScriptElement> parsedScriptPubKeyElements, out error))
         {
            return false;
         }

         ScriptEvaluationContext? legacyContext = null;

         // Taproot (P2TR - BIP341, BIP342) - Native SegWit v1 (OP_1 <32-byte_pubkey>)
         if (settings.IsTaprootEnabled && StandardScripts.IsPayToTaproot(parsedScriptPubKeyElements, out byte[]? taprootOutputKey))
         {
            if (!settings.IsWitnessEnabled) { error = ScriptError.WITNESS_UNEXPECTED; return false; } // Taproot requires witness
            if (parsedScriptSigElements.Count > 0) { error = ScriptError.WITNESS_MALLEATED; return false; } // Taproot scriptSig must be empty

            return ValidateTaprootSpend(txSpending, inputIndex, spentOutputs, taprootOutputKey!, txInput.ScriptWitness, settings, out error);
         }

         // SegWit v0 (P2WPKH, P2WSH)
         if (settings.IsWitnessEnabled)
         {
            if (StandardScripts.IsPayToWitnessPubKeyHash(parsedScriptPubKeyElements, out byte[]? pubKeyHashV0))
            {
               if (parsedScriptSigElements.Count > 0) { error = ScriptError.WITNESS_MALLEATED; return false; }
               return ValidateP2WPKH(txSpending, inputIndex, utxoAmount, pubKeyHashV0!, txInput.ScriptWitness, settings, out error);
            }
            if (StandardScripts.IsPayToWitnessScriptHash(parsedScriptPubKeyElements, out byte[]? scriptHashV0))
            {
               if (parsedScriptSigElements.Count > 0) { error = ScriptError.WITNESS_MALLEATED; return false; }
               return ValidateP2WSH(txSpending, inputIndex, utxoAmount, scriptHashV0!, txInput.ScriptWitness, settings, out error);
            }
         }

         // P2SH (BIP16) - Can wrap legacy, P2WPKH, or P2WSH
         if (settings.IsP2shEnabled && StandardScripts.IsPayToScriptHash(parsedScriptPubKeyElements, out byte[]? p2shScriptHash))
         {
            if (!StandardScripts.IsPushOnly(parsedScriptSigElements)) { error = ScriptError.SIG_PUSHONLY; return false; }
            if (parsedScriptSigElements.Count == 0 || parsedScriptSigElements.Last().Data == null) { error = ScriptError.INVALID_STACK_OPERATION; return false; }

            byte[] redeemScriptBytes = parsedScriptSigElements.Last().Data!;
            if (!ScriptParser.TryParse(redeemScriptBytes, out IList<ScriptElement> parsedRedeemScriptElements, out error)) return false;

            byte[] computedRedeemScriptHash = HashUtils.Hash160(redeemScriptBytes);
            if (!computedRedeemScriptHash.SequenceEqual(p2shScriptHash!)) { error = ScriptError.EQUALVERIFY_FAILED; return false; }

            // P2SH-wrapped SegWit v0
            if (settings.IsWitnessEnabled)
            {
               if (StandardScripts.IsPayToWitnessPubKeyHash(parsedRedeemScriptElements, out byte[]? p2sh_pubKeyHashV0))
               {
                  if (parsedScriptSigElements.Count != 1) { error = ScriptError.WITNESS_MALLEATED; return false; }
                  return ValidateP2WPKH(txSpending, inputIndex, utxoAmount, p2sh_pubKeyHashV0!, txInput.ScriptWitness, settings, out error, isP2SH: true);
               }
               if (StandardScripts.IsPayToWitnessScriptHash(parsedRedeemScriptElements, out byte[]? p2sh_scriptHashV0))
               {
                  if (parsedScriptSigElements.Count != 1) { error = ScriptError.WITNESS_MALLEATED; return false; }
                  return ValidateP2WSH(txSpending, inputIndex, utxoAmount, p2sh_scriptHashV0!, txInput.ScriptWitness, settings, out error, isP2SH: true);
               }
            }

            // Legacy P2SH evaluation
            var initialStackForRedeemScript = new Stack<byte[]>();
            for (int i = 0; i < parsedScriptSigElements.Count - 1; i++) initialStackForRedeemScript.Push(parsedScriptSigElements[i].Data ?? Array.Empty<byte>());

            legacyContext = new ScriptEvaluationContext(txSpending, inputIndex, utxoAmount, ScriptFlags.P2SH | (settings.IsWitnessEnabled ? ScriptFlags.Witness : ScriptFlags.None))
            { Script = parsedRedeemScriptElements };
            while (legacyContext.MainStack.Count > 0) legacyContext.MainStack.Pop();
            foreach (var item in initialStackForRedeemScript.Reverse()) legacyContext.MainStack.Push(item);
         }
         else // Not P2SH, not native SegWit v0, not native Taproot. Must be legacy (P2PK, P2PKH, P2MS, or non-standard).
         {
            if (settings.IsWitnessEnabled && (txInput.ScriptWitness?.Components?.Any() ?? false)) { error = ScriptError.WITNESS_UNEXPECTED; return false; }

            var combinedScriptElements = new List<ScriptElement>(parsedScriptSigElements); // scriptSig first
            combinedScriptElements.AddRange(parsedScriptPubKeyElements); // then scriptPubKey

            legacyContext = new ScriptEvaluationContext(txSpending, inputIndex, utxoAmount, ScriptFlags.None)
            { Script = combinedScriptElements };
         }

         // Perform evaluation for legacy P2SH or non-P2SH legacy scripts
         if (!_interpreter.Evaluate(legacyContext!))
         {
            error = legacyContext!.ScriptFailed ? ScriptError.EVAL_FALSE : ScriptError.UNKNOWN_ERROR; // TODO: Propagate error from context
            return false;
         }
         if (legacyContext.MainStack.Count == 0 || !ScriptInterpreter.CastToBool(legacyContext.MainStack.Peek()))
         {
            error = ScriptError.EVAL_FALSE; return false;
         }
         if (legacyContext.Flags.HasFlag(ScriptFlags.P2SH) && settings.IsWitnessEnabled && legacyContext.MainStack.Count != 1) // Stricter P2SH cleanstack if witness overall enabled
         {
             error = ScriptError.CLEANSTACK_VIOLATION; return false;
         }

         return true;
      }

      private bool ValidateP2WPKH(Transaction txSpending, int inputIndex, long utxoAmount, byte[] pubKeyHashV0FromScriptPubKey, ScriptWitness? witness, ScriptSettings settings, out ScriptError error, bool isP2SH = false)
      {
         error = ScriptError.OK;
         if (witness == null || witness.Components == null || witness.Components.Length != 2) { error = ScriptError.WITNESS_MALLEATED; return false; }
         byte[] signatureBytesWithSighashType = witness.Components[0].RawData!;
         byte[] fullPublicKeyBytes = witness.Components[1].RawData!;
         if (fullPublicKeyBytes.Length != 33 || (fullPublicKeyBytes[0] != 0x02 && fullPublicKeyBytes[0] != 0x03)) { error = ScriptError.WITNESS_PUBKEYTYPE; return false; }
         if (!HashUtils.Hash160(fullPublicKeyBytes).SequenceEqual(pubKeyHashV0FromScriptPubKey)) { error = ScriptError.WITNESS_PROGRAM_MISMATCH; return false; }

         var scriptCode = new List<byte>{ (byte)OpCodeType.OP_DUP, (byte)OpCodeType.OP_HASH160, 0x14 };
         scriptCode.AddRange(pubKeyHashV0FromScriptPubKey);
         scriptCode.Add((byte)OpCodeType.OP_EQUALVERIFY); scriptCode.Add((byte)OpCodeType.OP_CHECKSIG);
         byte[] scriptCodeBytes = scriptCode.ToArray();

         if (signatureBytesWithSighashType == null || signatureBytesWithSighashType.Length == 0) { error = ScriptError.SIG_DER_ENCODING_ERROR; return false; }
         byte sighashTypeByte = signatureBytesWithSighashType.Last();
         byte[] derSignature = signatureBytesWithSighashType.Take(signatureBytesWithSighashType.Length - 1).ToArray();

         byte[]? sighashToVerify = SighashGenerator.CalculateWitnessSignatureHash(txSpending, inputIndex, scriptCodeBytes, utxoAmount, sighashTypeByte);
         if (sighashToVerify == null) { error = ScriptError.SIG_HASHTYPE_ERROR; return false; }

         // ScriptFlags for CheckECDSASignature inside P2WPKH/P2WSH context should indicate it's a witnessV0 spend
         var contextForSigVerification = new ScriptEvaluationContext(txSpending, inputIndex, utxoAmount, ScriptFlags.WitnessV0);

         // This part is simplified. Real CheckECDSASignature is in ScriptInterpreter and needs context.
         // For now, directly calling the static BouncyCastle verifier.
         if (!Secp256k1BouncyCastle.VerifySignature(sighashToVerify, derSignature, fullPublicKeyBytes))
         { /* TODO: NullFail check */ error = ScriptError.CHECKSIGVERIFY_FAILED; return false; }
         return true;
      }

      private bool ValidateP2WSH(Transaction txSpending, int inputIndex, long utxoAmount, byte[] scriptHashV0FromScriptPubKey, ScriptWitness? witness, ScriptSettings settings, out ScriptError error, bool isP2SH = false)
      {
         error = ScriptError.OK;
         if (witness == null || witness.Components == null || witness.Components.Length == 0) { error = ScriptError.WITNESS_PROGRAM_WITNESS_EMPTY; return false; }
         byte[] witnessScriptBytes = witness.Components.Last().RawData!;
         if (witnessScriptBytes == null) { error = ScriptError.UNKNOWN_ERROR; return false; } // WITNESS_SCRIPT_EMPTY
         if (!HashUtils.Hash256(witnessScriptBytes).SequenceEqual(scriptHashV0FromScriptPubKey)) { error = ScriptError.WITNESS_PROGRAM_MISMATCH; return false; }

         var witnessStackForScript = new Stack<byte[]>();
         for (int i = 0; i < witness.Components.Length - 1; i++) witnessStackForScript.Push(witness.Components[i].RawData ?? Array.Empty<byte>());
         if (!ScriptParser.TryParse(witnessScriptBytes, out IList<ScriptElement> parsedWitnessScriptElements, out error)) return false;

         var witnessScriptContext = new ScriptEvaluationContext(txSpending, inputIndex, utxoAmount,
             (isP2SH ? ScriptFlags.P2SH : ScriptFlags.None) | ScriptFlags.Witness | ScriptFlags.WitnessV0)
            { Script = parsedWitnessScriptElements };
         while (witnessScriptContext.MainStack.Count > 0) witnessScriptContext.MainStack.Pop();
         foreach (var item in witnessStackForScript.Reverse()) witnessScriptContext.MainStack.Push(item);

         if (!_interpreter.Evaluate(witnessScriptContext))
         { error = witnessScriptContext.ScriptFailed ? ScriptError.EVAL_FALSE : ScriptError.UNKNOWN_ERROR; return false; } // Propagate error
         if (witnessScriptContext.MainStack.Count != 1 || !ScriptInterpreter.CastToBool(witnessScriptContext.MainStack.Peek()))
         { error = ScriptError.CLEANSTACK_VIOLATION; return false; }
         return true;
      }

      private bool ValidateTaprootSpend(Transaction txSpending, int inputIndex, TransactionOutput[] spentOutputs,
                                        byte[] taprootOutputKey, ScriptWitness? witness, ScriptSettings settings, out ScriptError error)
      {
         error = ScriptError.OK;
         if (witness == null || witness.Components == null || witness.Components.Length == 0)
         { error = ScriptError.WITNESS_PROGRAM_WITNESS_EMPTY; return false; } // Taproot always needs witness

         var witnessStack = new List<byte[]>(witness.Components.Select(c => c.RawData ?? Array.Empty<byte>()));
         byte[]? annex = null;
         if (witnessStack.Count >= 2 && witnessStack.Last().Length > 0 && witnessStack.Last()[0] == 0x50) // Taproot annex prefix
         {
            annex = witnessStack.Last();
            witnessStack.RemoveAt(witnessStack.Count - 1);
            if(witnessStack.Count == 0) { error = ScriptError.WITNESS_MALLEATED; return false; } // Annex cannot be the only element
         }

         if (witnessStack.Count == 1) // Potential Key Path Spend
         {
            return ValidateTaprootKeyPathSpend(txSpending, inputIndex, spentOutputs, taprootOutputKey, witnessStack[0], annex, settings, out error);
         }
         else // Potential Script Path Spend
         {
            // TODO: Implement ValidateTaprootScriptPathSpend (Task 3.3)
            error = ScriptError.UNKNOWN_ERROR; // Not yet implemented
            return false;
         }
      }

      private bool ValidateTaprootKeyPathSpend(Transaction txSpending, int inputIndex, TransactionOutput[] spentOutputs,
                                             byte[] taprootOutputKey, byte[] signatureWithSighashType, byte[]? annex,
                                             ScriptSettings settings, out ScriptError error)
      {
         error = ScriptError.OK;
         if (signatureWithSighashType == null || signatureWithSighashType.Length == 0) { error = ScriptError.TAPROOT_KEY_PATH_NO_SIGNATURE; return false; }

         byte sighashTypeRaw = 0x00; // Default SIGHASH_ALL_TAPROOT
         byte[] schnorrSignature;

         if (signatureWithSighashType.Length == 64) schnorrSignature = signatureWithSighashType;
         else if (signatureWithSighashType.Length == 65)
         {
            sighashTypeRaw = signatureWithSighashType.Last();
            if (sighashTypeRaw == 0x00) { error = ScriptError.SIG_HASHTYPE_ERROR; return false; } // Must not be default if specified
            schnorrSignature = signatureWithSighashType.Take(64).ToArray();
         }
         else { error = ScriptError.TAPROOT_KEY_PATH_SIGNATURE_INVALID_LENGTH; return false; }

         byte[]? taprootSighash = SighashGenerator.CalculateTaprootSignatureHash(txSpending, inputIndex, spentOutputs,
                                                                                sighashTypeRaw, extFlags: 0, tapLeafHash: null, annex: annex);
         if (taprootSighash == null) { error = ScriptError.SIG_HASHTYPE_ERROR; return false; }

         if (!SchnorrVerifierBouncyCastle.VerifyBip340Signature(taprootSighash, schnorrSignature, taprootOutputKey))
         { error = ScriptError.SCHNORR_SIG_VERIFICATION_FAILED; return false; }

         return true;
      }
   }
}

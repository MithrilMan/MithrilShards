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
         else if (witnessStack.Count >= 2) // Potential Script Path Spend (must have at least script and control block)
         {
            byte[] controlBlockBytes = witnessStack.Last(); // Last element is control block
            witnessStack.RemoveAt(witnessStack.Count - 1);

            byte[] tapScriptBytes = witnessStack.Last(); // Second to last is script
            witnessStack.RemoveAt(witnessStack.Count - 1);

            // Remaining items in witnessStack are the initial stack for the Tapscript
            List<byte[]> initialStackForTapscript = witnessStack;

            return ValidateTaprootScriptPathSpend(txSpending, inputIndex, spentOutputs, taprootOutputKey,
                                                controlBlockBytes, tapScriptBytes, initialStackForTapscript,
                                                annex, settings, out error);
         }
         else // Invalid witness stack for Taproot (e.g. empty after annex removal and not key-path)
         {
            error = ScriptError.WITNESS_MALLEATED; // Or a more specific Taproot error
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

      private bool ValidateTaprootScriptPathSpend(Transaction txSpending,
                                                int inputIndex,
                                                TransactionOutput[] spentOutputs,
                                                byte[] taprootOutputKeyFromScriptPubKey,
                                                byte[] controlBlockBytes,
                                                byte[] tapScriptBytes,
                                                List<byte[]> initialStackForTapscript,
                                                byte[]? annex,
                                                ScriptSettings settings,
                                                out ScriptError error)
      {
         error = ScriptError.OK;
         // _logger?.LogDebug("Taproot Script Path spend validation STUBBED.");

         // a. Parse Control Block
         if (controlBlockBytes == null || controlBlockBytes.Length < 33 || controlBlockBytes.Length > 33 + 128 * 32 || (controlBlockBytes.Length - 33) % 32 != 0)
         {
            error = ScriptError.TAPROOT_WRONG_CONTROL_BLOCK_SIZE;
            return false;
         }

         byte leafVersionAndParity = controlBlockBytes[0];
         byte tapLeafVersion = (byte)(leafVersionAndParity & 0xFE);
         // byte parityBit = (byte)(leafVersionAndParity & 0x01); // Needed for commitment check

         if (tapLeafVersion != 0xc0) // LEAF_VERSION_TAPSCRIPT
         {
            // For future leaf versions, if SCRIPT_VERIFY_DISCOURAGE_UPGRADABLE_TAPROOT_VERSION is set, fail.
            // Otherwise, treat as success (anyone-can-spend).
            // For now, we only support 0xc0.
            error = ScriptError.TAPROOT_INVALID_LEAF_VERSION;
            return false;
         }

         byte[] internalPublicKeyBytes_p = new byte[32];
         System.Buffer.BlockCopy(controlBlockBytes, 1, internalPublicKeyBytes_p, 0, 32);

         List<byte[]> merklePathHashes = new List<byte[]>();
         for (int i = 33; i < controlBlockBytes.Length; i += 32)
         {
            byte[] hash = new byte[32];
            System.Buffer.BlockCopy(controlBlockBytes, i, hash, 0, 32);
            merklePathHashes.Add(hash);
         }
         if (merklePathHashes.Count > 128) // Max Merkle path depth
         {
             error = ScriptError.TAPROOT_CONTROL_BLOCK_MERKLE_PROOF_INVALID;
             return false;
         }

         byte parityBit = (byte)(leafVersionAndParity & 0x01);

         // b. Verify Taproot Commitment
         // b.1. Calculate tapLeafHash = TaggedHash("TapLeaf", tap_leaf_version || CompactSize(tapscript) || tapscript)
         byte[] tapLeafPreimage;
         using (var ms = new System.IO.MemoryStream())
         using (var writer = new System.IO.BinaryWriter(ms))
         {
            writer.Write(tapLeafVersion);
            SighashGenerator.WriteTaprootCompactSizeForTest(writer, (ulong)tapScriptBytes.Length); // Assuming this helper is accessible or reimplemented
            writer.Write(tapScriptBytes);
            tapLeafPreimage = ms.ToArray();
         }
         byte[] tapLeafHash = HashUtils.TaggedHash("TapLeaf", tapLeafPreimage);

         // b.2. Calculate Merkle Root (tweak_t) from tapLeafHash and Merkle path
         byte[] currentHash = tapLeafHash;
         foreach (byte[] pathElement in merklePathHashes)
         {
            int comparison = CompareByteArrays(currentHash, pathElement);
            byte[] combined;
            if (comparison < 0) // currentHash is smaller
            {
               combined = currentHash.Concat(pathElement).ToArray();
            }
            else // pathElement is smaller or equal
            {
               combined = pathElement.Concat(currentHash).ToArray();
            }
            currentHash = HashUtils.TaggedHash("TapBranch", combined);
         }
         byte[] merkleRoot_t_bytes = currentHash;

         // b.3. Reconstruct and Verify Taproot Output Key Q
         //    P_internal = lift_x(internalPublicKeyBytes_p)
         Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters? pInternalParams = SchnorrVerifierBouncyCastle.ParseXOnlyPublicKey_BouncyCastle(internalPublicKeyBytes_p);
         if (pInternalParams == null)
         {
            error = ScriptError.TAPROOT_CONTROL_BLOCK_INVALID_PUBKEY;
            return false;
         }
         Org.BouncyCastle.Math.EC.ECPoint P_internal = pInternalParams.Q;

         //    tweak_scalar = int(merkleRoot_t_bytes) mod n
         Org.BouncyCastle.Math.BigInteger tweak_scalar = new Org.BouncyCastle.Math.BigInteger(1, merkleRoot_t_bytes);
         if (tweak_scalar.CompareTo(SchnorrVerifierBouncyCastle.OrderN_BouncyCastle) >= 0)
         {
            error = ScriptError.TAPROOT_COMMITMENT_MISMATCH; // Tweak out of range
            return false;
         }

         //    Q_calculated_point = P_internal + tweak_scalar * G
         Org.BouncyCastle.Math.EC.ECPoint G = SchnorrVerifierBouncyCastle.DomainParameters_BouncyCastle.G;
         Org.BouncyCastle.Math.EC.ECPoint Q_calculated_point = P_internal.Add(G.Multiply(tweak_scalar)).Normalize();

         if (Q_calculated_point.IsInfinity)
         {
            error = ScriptError.TAPROOT_COMMITMENT_MISMATCH; // Resulting point is infinity
            return false;
         }

         // Compare x(Q_calculated_point) with taprootOutputKeyFromScriptPubKey (which is x(Q_expected))
         byte[] x_q_calculated_bytes = Q_calculated_point.XCoord.ToBigInteger().ToByteArrayUnsigned();
         // Ensure 32 bytes by padding with leading zeros if necessary (though unlikely for valid X)
         if (x_q_calculated_bytes.Length < 32)
         {
            byte[] padded = new byte[32];
            System.Buffer.BlockCopy(x_q_calculated_bytes, 0, padded, 32 - x_q_calculated_bytes.Length, x_q_calculated_bytes.Length);
            x_q_calculated_bytes = padded;
         }
         else if (x_q_calculated_bytes.Length > 32) // Should not happen for secp256k1 x-coordinate
         {
             error = ScriptError.TAPROOT_COMMITMENT_MISMATCH; return false;
         }


         if (!x_q_calculated_bytes.SequenceEqual(taprootOutputKeyFromScriptPubKey))
         {
            error = ScriptError.TAPROOT_COMMITMENT_MISMATCH;
            return false;
         }

         // Check parity
         bool y_is_odd = Q_calculated_point.YCoord.ToBigInteger().TestBit(0);
         if ((parityBit == 1) != y_is_odd)
         {
            error = ScriptError.TAPROOT_COMMITMENT_MISMATCH; // Parity mismatch
            return false;
         }
         // Commitment verified.

         // c. Execute Tapscript
         // TODO: This is the next major step.
         // For now, if commitment is verified, we'll consider script path spend valid up to this point.
         // A full implementation will parse tapScriptBytes, create ScriptEvaluationContext,
         // populate stack with initialStackForTapscript, and run _interpreter.Evaluate for Tapscript rules.

         System.Diagnostics.Debug.WriteLine("WARNING: Tapscript execution part of ValidateTaprootScriptPathSpend is STUBBED.");
         // error = ScriptError.UNKNOWN_ERROR; // Mark as not fully implemented for Tapscript execution
         // return false; // Returning false until Tapscript execution is also implemented

         // For this subtask, successfully verifying the commitment is the goal.
         // The actual Tapscript execution is for Task 3.3d.
         // So, if we reach here, the commitment part is valid.

         // c. Execute Tapscript
         if (tapScriptBytes.Length > ScriptInterpreter.MAX_SCRIPT_SIZE)
         {
            error = ScriptError.SCRIPT_SIZE_EXCEEDED;
            return false;
         }

         if (!ScriptParser.TryParse(tapScriptBytes, out IList<ScriptElement> parsedTapscriptElements, out ScriptError parseError))
         {
            error = parseError; // Propagate parsing error
            return false;
         }

         var tapScriptContext = new ScriptEvaluationContext(
             txSpending,
             inputIndex,
             spentOutputs[inputIndex].Value, // Amount for the current input
             ScriptFlags.Taproot | ScriptFlags.Witness, // Base flags for Tapscript
                                                        // Add any other relevant flags from 'settings' if needed, e.g. settings.ScriptVerifyFlags
             allSpentOutputs: spentOutputs,
             currentTapLeafHash: tapLeafHash, // Calculated during commitment verification
             currentTapLeafVersion: tapLeafVersion, // From control block (e.g., 0xc0)
             annexForSighash: annex // Raw annex bytes
         );
         tapScriptContext.Script = parsedTapscriptElements;

         // Populate initial stack for Tapscript
         // As per BIP342, stack items are pushed in reverse order of their appearance in the witness.
         // `initialStackForTapscript` is already in the correct order (script's initial stack items, last one on top).
         // So, we push them onto the context's MainStack in the order they are, which means they'll be reversed on the stack.
         // Then, when popped by script opcodes, they come out in the intended order.
         // Example: witness: [... S2 S1 CtlBlk Script], initialStackForTapscript = [S2, S1]. Stack becomes [S1 (top), S2].
         while (tapScriptContext.MainStack.Count > 0) tapScriptContext.MainStack.Pop(); // Clear stack
         foreach (var item in initialStackForTapscript.AsEnumerable().Reverse()) // Iterate from bottom of initial witness stack to top
         {
            // BIP342: "The existing 520 byte limit on stack element size applies."
            if (item.Length > ScriptInterpreter.MAX_SCRIPT_ELEMENT_SIZE_TAPSCRIPT)
            {
               error = ScriptError.TAPROOT_STACK_ELEMENT_SIZE_EXCEEDED_TAPSCRIPT;
               return false;
            }
            tapScriptContext.MainStack.Push(item);
         }

         // Execute the Tapscript
         if (!_interpreter.Evaluate(tapScriptContext))
         {
            error = tapScriptContext.GetError(); // Propagate specific error from interpreter
            if (error == ScriptError.OK && tapScriptContext.ScriptFailed) // Ensure an error is set if script failed but OK was returned
            {
               error = ScriptError.EVAL_FALSE; // Generic failure if no specific error was set
            }
            return false;
         }

         // The _interpreter.Evaluate now handles the final stack state check for Taproot
         // (exactly one true item, or OP_SUCCESS path).
         // If it returned true, the Tapscript is considered valid.
         return true;
      }

      // Helper for lexicographical byte array comparison
      private int CompareByteArrays(byte[] a, byte[] b)
      {
         int len = System.Math.Min(a.Length, b.Length);
         for (int i = 0; i < len; i++)
         {
            if (a[i] < b[i]) return -1;
            if (a[i] > b[i]) return 1;
         }
         return a.Length.CompareTo(b.Length);
      }
   }
}

using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes; // For Amount, if it's there, or define a simple long.

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Script
{
   public class ScriptEvaluationContext
   {
      /// <summary>
      /// The main script evaluation stack. Items are byte arrays.
      /// </summary>
      public Stack<byte[]> MainStack { get; } = new Stack<byte[]>();

      /// <summary>
      /// The alternate script evaluation stack.
      /// </summary>
      public Stack<byte[]> AltStack { get; } = new Stack<byte[]>();

      /// <summary>
      /// The script being executed, parsed into elements.
      /// This will be populated by a script parser in a later task.
      /// For now, it might be set directly in tests or by a rudimentary parser.
      /// </summary>
      public IReadOnlyList<ScriptElement> Script { get; set; } = System.Array.Empty<ScriptElement>();

      /// <summary>
      /// The current instruction pointer (index into the Script list).
      /// </summary>
      public int ProgramCounter { get; set; } = 0;

      /// <summary>
      /// Flag indicating if script execution has failed.
      /// </summary>
      public bool ScriptFailed { get; set; } = false;

      /// <summary>
      /// Flag indicating if script execution should be immediately halted (e.g. after OP_RETURN or a failed OP_VERIFY).
      /// </summary>
      public bool HaltExecution { get; set; } = false;

      /// <summary>
      /// The transaction containing the input being validated.
      /// (Required for CHECKSIG, but added now for context completeness).
      /// </summary>
      public Transaction? Transaction { get; }

      /// <summary>
      /// The index of the input in the transaction that is being validated.
      /// (Required for CHECKSIG).
      /// </summary>
      public int InputIndex { get; }

      /// <summary>
      /// The amount of the output being spent.
      /// (Required for CHECKSIG in SegWit context).
      /// For now, using long. Could be replaced with a proper Amount type if available.
      /// </summary>
      public long Amount { get; } // TODO: Replace with proper Amount type if available from Core.DataTypes

      /// <summary>
      /// Script evaluation flags (e.g., P2SH, WITNESS, etc.).
      /// For now, this is a placeholder. Specific flags will be defined and used later.
      /// </summary>
      public ScriptFlags Flags { get; }

      /// <summary>
      /// Manages the state of conditional execution (OP_IF, OP_ELSE, OP_ENDIF).
      /// Stores true if currently in an executing branch, false otherwise.
      /// </summary>
      public Stack<bool> ConditionalsStack { get; } = new Stack<bool>();

      /// <summary>
      /// The index of the last executed OP_CODESEPARATOR.
      /// Used in signature checking operations to determine the scriptCode.
      /// Initialized to 0, meaning the entire script from the beginning.
      /// </summary>
      public int CodeSeparatorPosition { get; set; } = 0;


      public ScriptEvaluationContext(Transaction? transaction, int inputIndex, long amount, ScriptFlags flags = ScriptFlags.None)
      {
         Transaction = transaction;
         InputIndex = inputIndex;
         Amount = amount;
         Flags = flags;
      }

      public ScriptEvaluationContext(ScriptFlags flags = ScriptFlags.None)
      : this(null, -1, 0, flags) { }


      /// <summary>
      /// Marks the script as failed and sets HaltExecution to true.
      /// </summary>
      public void SetError(ScriptError error = ScriptError.UNKNOWN_ERROR) // TODO: Define ScriptError enum
      {
         // In a real system, 'error' would be recorded.
         ScriptFailed = true;
         HaltExecution = true;
      }

      /// <summary>
      /// Gets the current conditional state (i.e. if the current branch of an IF/ELSE should execute).
      /// Returns true if not inside any conditional block or if the current block is executing.
      /// </summary>
      public bool IsBranchExecuting()
      {
         foreach (bool condition in ConditionalsStack)
         {
            if (!condition) return false; // If any condition on stack is false, this branch is not executing
         }
         return true; // All conditions are true, or stack is empty
      }
   }

   [System.Flags]
   public enum ScriptFlags
   {
      None = 0,
      P2SH = 1 << 0,                       // Evaluate P2SH script hashing.
      Witness = 1 << 1,                    // General witness program validation. (Might be redundant if WitnessV0 is more specific)
      WitnessV0 = 1 << 2,                  // Specific to SegWit Version 0 (P2WPKH, P2WSH) validation rules.
      StrictEncoding = 1 << 3,             // SCRIPT_VERIFY_STRICTENC / SCRIPT_VERIFY_DERSIG + SCRIPT_VERIFY_LOW_S + SCRIPT_VERIFY_NULLDUMMY
      MinimalData = 1 << 4,                // SCRIPT_VERIFY_MINIMALDATA
      DiscourageUpgradableWitness = 1 << 5, // SCRIPT_VERIFY_DISCOURAGE_UPGRADABLE_WITNESS_PROGRAM
      CleanStack = 1 << 6,                 // SCRIPT_VERIFY_CLEANSTACK (P2WSH rule, also for P2SH)
      NullFail = 1 << 7,                   // SCRIPT_VERIFY_NULLFAIL (signature must be empty if validation fails)
      CheckLockTimeVerify = 1 << 8,        // SCRIPT_VERIFY_CHECKLOCKTIMEVERIFY
      CheckSequenceVerify = 1 << 9,        // SCRIPT_VERIFY_CHECKSEQUENCEVERIFY
      Taproot = 1 << 10,                   // SCRIPT_VERIFY_TAPROOT (BIP341/BIP342)
      // Add more flags as other BIPs are implemented
   }

   // Consolidated ScriptError enum
   public enum ScriptError
   {
      OK = 0, // No error

      // Evaluation failures
      UNKNOWN_ERROR,
      EVAL_FALSE,            // Top of stack was false when true was expected
      OP_RETURN,             // Encountered OP_RETURN

      // Max size errors
      SCRIPT_SIZE_EXCEEDED,
      STACK_SIZE_EXCEEDED,
      OP_COUNT_EXCEEDED,
      DATA_TOO_LARGE,        // Pushed data element too large

      // Failed verify operations
      VERIFY_FAILED,
      EQUALVERIFY_FAILED,
      CHECKSIGVERIFY_FAILED,
      CHECKMULTISIGVERIFY_FAILED,
      NUMEQUALVERIFY_FAILED,

      // Logical/Format errors
      BAD_OPCODE,
      DISABLED_OPCODE,
      INVALID_STACK_OPERATION,
      INVALID_ALTSTACK_OPERATION,
      UNBALANCED_CONDITIONAL, // OP_IF/OP_NOTIF without OP_ENDIF

      // Signature validation errors
      SIG_HASHTYPE_ERROR,
      SIG_DER_ENCODING_ERROR,    // Error in DER signature parsing
      SIG_INVALID_PUBKEY_FORMAT,
      SIG_NULLFAIL,            // Signature failed an SCRIPT_VERIFY_NULLFAIL check.

      // Softfork safeness checks
      DISCOURAGE_UPGRADABLE_WITNESS_PROGRAM,

      // Witness specific errors (BIP141)
      WITNESS_PROGRAM_WRONG_LENGTH, // Witness program has incorrect length for its version
      WITNESS_PROGRAM_WITNESS_EMPTY, // Witness program was segwit v0, but stack is empty
      WITNESS_PROGRAM_MISMATCH,     // Witness program hash doesn't match P2WSH script or P2WPKH pubkey
      WITNESS_MALLEATED,            // Witness components have unexpected data (e.g. P2WPKH stack size)
      WITNESS_UNEXPECTED,           // Witness data found for non-witness script
      WITNESS_PUBKEYTYPE,           // Public key type not supported by SegWit (e.g. uncompressed in v0)

      // P2SH specific errors
      SIG_PUSHONLY,            // Non-push operation in scriptSig for P2SH
      CLEANSTACK_VIOLATION,    // Stack not clean after script execution (P2SH or P2WSH)

      // Minimal data errors
      MINIMALDATA_ENCODING_ERROR, // Data push not minimally encoded

      // CLTV/CSV errors
      NEGATIVE_LOCKTIME,
      UNSATISFIED_LOCKTIME,

      // Parsing specific errors (can also be caught during execution if parsing on the fly)
      PUSHDATA_SIZE_ERROR,       // Error in OP_PUSHDATA (e.g. length byte indicates more data than available)
      UNEXPECTED_END_OF_SCRIPT,  // Script ended prematurely during a multi-byte operation

      // Taproot specific errors (BIP341/342)
      TAPROOT_WRONG_CONTROL_BLOCK_SIZE,
      TAPROOT_INVALID_LEAF_VERSION,
      TAPROOT_CHECKSIGADD_FAIL, // From OP_CHECKSIGADD
      SCHNORR_SIG_VERIFICATION_FAILED, // Generic Schnorr failure if not covered by more specific ones
      SCHNORR_SIG_INVALID_LENGTH,
      SCHNORR_SIG_INVALID_PUBKEY_X_ONLY,
      SCHNORR_SIG_R_COMPONENT_INVALID, // R >= N or R not on curve (BIP340 implies R is an x-coord, so always on curve if valid x)
      SCHNORR_SIG_S_COMPONENT_INVALID, // S >= N
      TAPROOT_KEY_PATH_NO_SIGNATURE, // Key path spend with no signature
      TAPROOT_KEY_PATH_SIGNATURE_INVALID_LENGTH, // Key path spend with signature of wrong length (not 64 or 65)
   }
}

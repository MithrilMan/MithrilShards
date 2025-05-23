using System.Collections.Generic;
using System.Linq;
using MithrilShards.Chain.Bitcoin.Protocol.Types; // For OpCodeType

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Script
{
   /// <summary>
   /// Utility class to identify and work with standard Bitcoin script patterns.
   /// </summary>
   public static class StandardScripts
   {
      /// <summary>
      /// Checks if the script matches the Pay-to-Public-Key (P2PK) pattern:
      /// <pubkey> OP_CHECKSIG
      /// </summary>
      public static bool IsPayToPubKey(IList<ScriptElement> scriptElements, out byte[]? pubKey)
      {
         pubKey = null;
         if (scriptElements.Count == 2 &&
             scriptElements[0].IsPushData() && (scriptElements[0].Data?.Length == 33 || scriptElements[0].Data?.Length == 65) && // Compressed or Uncompressed PubKey
             scriptElements[1].OpCode == OpCodeType.OP_CHECKSIG)
         {
            pubKey = scriptElements[0].Data;
            return true;
         }
         return false;
      }

      /// <summary>
      /// Checks if the script matches the Pay-to-Public-Key-Hash (P2PKH) pattern:
      /// OP_DUP OP_HASH160 <20-byte-pubkey-hash> OP_EQUALVERIFY OP_CHECKSIG
      /// </summary>
      public static bool IsPayToPubKeyHash(IList<ScriptElement> scriptElements, out byte[]? pubKeyHash)
      {
         pubKeyHash = null;
         if (scriptElements.Count == 5 &&
             scriptElements[0].OpCode == OpCodeType.OP_DUP &&
             scriptElements[1].OpCode == OpCodeType.OP_HASH160 &&
             scriptElements[2].IsPushData() && scriptElements[2].Data?.Length == 20 &&
             scriptElements[3].OpCode == OpCodeType.OP_EQUALVERIFY &&
             scriptElements[4].OpCode == OpCodeType.OP_CHECKSIG)
         {
            pubKeyHash = scriptElements[2].Data;
            return true;
         }
         return false;
      }

      /// <summary>
      /// Checks if the script matches the Pay-to-Script-Hash (P2SH) pattern:
      /// OP_HASH160 <20-byte-script-hash> OP_EQUAL
      /// </summary>
      public static bool IsPayToScriptHash(IList<ScriptElement> scriptElements, out byte[]? scriptHash)
      {
         scriptHash = null;
         if (scriptElements.Count == 3 &&
             scriptElements[0].OpCode == OpCodeType.OP_HASH160 &&
             scriptElements[1].IsPushData() && scriptElements[1].Data?.Length == 20 &&
             scriptElements[2].OpCode == OpCodeType.OP_EQUAL)
         {
            scriptHash = scriptElements[1].Data;
            return true;
         }
         return false;
      }

      /// <summary>
      /// Checks if the script matches the Pay-to-MultiSig (P2MS) pattern:
      /// m <pubkey1> <pubkey2> ... <pubkeyN> n OP_CHECKMULTISIG
      /// Where m and n are OP_1 to OP_16 or small pushes.
      /// </summary>
      public static bool IsPayToMultiSig(IList<ScriptElement> scriptElements, out int m, out int n, out List<byte[]>? pubKeys)
      {
         m = 0;
         n = 0;
         pubKeys = null;

         if (scriptElements.Count < 3) return false; // At least m, n, and OP_CHECKMULTISIG, plus one pubkey. So min 4? (OP_1 <pk> OP_1 OP_CMS)

         if (scriptElements.Last().OpCode != OpCodeType.OP_CHECKMULTISIG) return false;

         // Get N (number of public keys)
         ScriptElement nElement = scriptElements[scriptElements.Count - 2];
         if (!TryGetScriptNumber(nElement, out n) || n < 0 || n > 16) return false; // Max 16 pubkeys in standard P2MS

         // Get M (number of required signatures)
         ScriptElement mElement = scriptElements[0];
         if (!TryGetScriptNumber(mElement, out m) || m < 0 || m > n) return false;

         // Number of pubkeys should be n. Total elements = 1 (m) + n (pubkeys) + 1 (n_opcode) + 1 (OP_CHECKMULTISIG)
         if (scriptElements.Count != (1 + n + 1 + 1)) return false;

         pubKeys = new List<byte[]>(n);
         for (int i = 0; i < n; i++)
         {
            ScriptElement pubKeyElement = scriptElements[1 + i]; // Pubkeys start after m
            if (!pubKeyElement.IsPushData() ||
                !(pubKeyElement.Data?.Length == 33 || pubKeyElement.Data?.Length == 65)) // Compressed or uncompressed
            {
               // Invalid pubkey
               pubKeys = null; // ensure out param is null on failure
               return false;
            }
            pubKeys.Add(pubKeyElement.Data);
         }

         return true;
      }

      /// <summary>
      /// Tries to get a number from a script element (OP_0-OP_16 or a data push representing a number).
      /// </summary>
      private static bool TryGetScriptNumber(ScriptElement element, out int number)
      {
         number = 0;
         if (element.OpCode >= OpCodeType.OP_1 && element.OpCode <= OpCodeType.OP_16)
         {
            number = (element.OpCode - OpCodeType.OP_1) + 1;
            return true;
         }
         if (element.OpCode == OpCodeType.OP_0)
         {
            number = 0;
            return true;
         }
         // For P2MS, m and n are usually small numbers represented by OP_1 to OP_16.
         // More general number parsing from data push is not typically needed for standard P2MS m/n.
         // If they were arbitrary pushes, use ScriptNum(element.Data, true).Value.
         // For now, restrict to OP_0-OP_16 for simplicity in P2MS m/n detection.
         return false;
      }


      /// <summary>
      /// Checks if a script (typically a scriptSig) consists only of data push operations.
      /// This is a requirement for P2SH scriptSigs (before the redeemScript itself).
      /// OP_0 through OP_16 are considered data pushes. OP_1NEGATE is also a data push.
      /// Larger numbers pushed via OP_PUSHDATAx are also data pushes.
      /// It must not contain any other opcodes.
      /// </summary>
      public static bool IsPushOnly(IList<ScriptElement> scriptElements)
      {
         if (scriptElements == null) return false; // Or true for empty? Context dependent. Let's say false for null.
         if (!scriptElements.Any()) return true; // Empty script is push-only.

         foreach (ScriptElement element in scriptElements)
         {
            // IsPushData() in ScriptElement covers OP_0-OP_16, OP_1NEGATE, and actual data pushes.
            // We must also ensure that the opcode value itself is not > OP_16 if it's not an actual PUSHDATA opcode.
            // This means an opcode like OP_ADD (0x93) is not a push.
            // ScriptElement.IsPushData() should correctly identify this.
            if (!element.IsPushData())
            {
               // However, some opcodes like OP_RESERVED might be misclassified by a simple IsPushData if their byte value is low.
               // ScriptElement's IsPushData relies on Data field or specific opcodes.
               // A strict check ensures it's either a defined PUSHDATA op (OP_PUSHDATA1/2/4)
               // or its value is <= OP_16 (which includes OP_0 to OP_16 and direct pushes 1-75 bytes).
               // This means any opcode > OP_16 that isn't OP_PUSHDATA1/2/4 or OP_1NEGATE is not a push.
               if (element.OpCode > OpCodeType.OP_16 &&
                   element.OpCode != OpCodeType.OP_PUSHDATA1 &&
                   element.OpCode != OpCodeType.OP_PUSHDATA2 &&
                   element.OpCode != OpCodeType.OP_PUSHDATA4 &&
                   element.OpCode != OpCodeType.OP_1NEGATE
                  )
               {
                  // Check if it's a direct push (opcode value is the length)
                  if (!((int)element.OpCode >= 1 && (int)element.OpCode <= 75)) // 0x01 to 0x4b
                  {
                     return false;
                  }
               }
               // If element.Data is null for opcodes like OP_ADD, IsPushData() in ScriptElement should be false.
               // Let's rely on ScriptElement.IsPushData() as the primary check.
               if (!element.IsPushData()) return false;
            }
         }
         return true;
      }

      /// <summary>
      /// Checks if the script matches the Pay-to-Witness-Public-Key-Hash (P2WPKH) pattern:
      /// OP_0 <20-byte-pubkey-hash>
      /// </summary>
      public static bool IsPayToWitnessPubKeyHash(IList<ScriptElement> scriptElements, out byte[]? pubKeyHashV0)
      {
         pubKeyHashV0 = null;
         if (scriptElements.Count == 2 &&
             scriptElements[0].OpCode == OpCodeType.OP_0 && // Witness version 0
             scriptElements[1].IsPushData() && scriptElements[1].Data?.Length == 20) // 20-byte hash
         {
            pubKeyHashV0 = scriptElements[1].Data;
            return true;
         }
         return false;
      }

      /// <summary>
      /// Checks if the script matches the Pay-to-Witness-Script-Hash (P2WSH) pattern:
      /// OP_0 <32-byte-script-hash>
      /// </summary>
      public static bool IsPayToWitnessScriptHash(IList<ScriptElement> scriptElements, out byte[]? scriptHashV0)
      {
         scriptHashV0 = null;
         if (scriptElements.Count == 2 &&
             scriptElements[0].OpCode == OpCodeType.OP_0 && // Witness version 0
             scriptElements[1].IsPushData() && scriptElements[1].Data?.Length == 32) // 32-byte hash (SHA256)
         {
            scriptHashV0 = scriptElements[1].Data;
            return true;
         }
         return false;
      }

      /// <summary>
      /// Checks if the script matches the Pay-to-Taproot (P2TR) pattern:
      /// OP_1 <32-byte-output-key>
      /// </summary>
      public static bool IsPayToTaproot(IList<ScriptElement> scriptPubKeyElements, out byte[]? taprootOutputKey)
      {
         taprootOutputKey = null;
         if (scriptPubKeyElements.Count == 2 &&
             scriptPubKeyElements[0].OpCode == OpCodeType.OP_1 && // Witness version 1 for Taproot
             scriptPubKeyElements[1].IsPushData() && scriptPubKeyElements[1].Data?.Length == 32) // 32-byte Taproot output key (x-only public key)
         {
            taprootOutputKey = scriptPubKeyElements[1].Data;
            return true;
         }
         return false;
      }
   }
}

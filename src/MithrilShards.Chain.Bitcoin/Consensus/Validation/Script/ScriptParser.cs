using System;
using System.Collections.Generic;
using System.Linq;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Script
{
   public static class ScriptParser
   {
      private const int MAX_SCRIPT_SIZE_CONSENSUS = 10000; // Max script size (consensus rule)
      private const int MAX_SCRIPT_ELEMENT_SIZE = 520;   // Max size of an element (opcode or data push)

      public static bool TryParse(byte[] rawScript, out IList<ScriptElement> parsedScriptElements, out ScriptError error)
      {
         parsedScriptElements = new List<ScriptElement>();
         error = ScriptError.UNKNOWN_ERROR; // Default error

         if (rawScript == null)
         {
            // Or handle as empty script? Bitcoin Core treats null script as valid empty script in some contexts.
            // For parsing, let's assume non-null. Caller can handle null.
            // For now, let's treat null as an error for TryParse itself.
            // However, an empty rawScript is a valid empty script.
            error = ScriptError.UNKNOWN_ERROR; // Or a new SCRIPT_NULL error
            return false;
         }

         if (rawScript.Length > MAX_SCRIPT_SIZE_CONSENSUS)
         {
            error = ScriptError.SCRIPT_SIZE_EXCEEDED;
            return false;
         }

         if (rawScript.Length == 0)
         {
            // Empty script is valid, results in an empty list of elements.
            error = ScriptError.UNKNOWN_ERROR; // No error, but TryParse needs to indicate success
            return true; // Successfully parsed an empty script
         }

         int pc = 0;
         while (pc < rawScript.Length)
         {
            OpCodeType opcode = (OpCodeType)rawScript[pc];
            pc++;

            if (opcode >= OpCodeType.OP_0 && opcode <= OpCodeType.OP_PUSHDATA4) // Data pushing opcodes
            {
               int dataLength = -1;
               byte[]? dataToPush = null;

               if (opcode >= OpCodeType.OP_0 && opcode < OpCodeType.OP_PUSHDATA1) // Direct push: 0x00 is OP_0 (empty push), 0x01-0x4b is push N bytes
               {
                  dataLength = (int)opcode; // For OP_0, length is 0. For 0x01-0x4b, length is opcode value.
                  // OP_0 is special, it pushes an empty byte array.
                  // For 0x01 to 0x4b (75), the opcode itself is the length.
                  if (opcode == OpCodeType.OP_0)
                  {
                     dataToPush = System.Array.Empty<byte>();
                  }
                  // else, dataLength is already set to opcode value for 0x01-0x4b
               }
               else if (opcode == OpCodeType.OP_PUSHDATA1)
               {
                  if (pc >= rawScript.Length) { error = ScriptError.UNEXPECTED_END_OF_SCRIPT; return false; }
                  dataLength = rawScript[pc];
                  pc++;
               }
               else if (opcode == OpCodeType.OP_PUSHDATA2)
               {
                  if (pc + 1 >= rawScript.Length) { error = ScriptError.UNEXPECTED_END_OF_SCRIPT; return false; }
                  dataLength = (rawScript[pc + 1] << 8) | rawScript[pc]; // Little-endian ushort
                  pc += 2;
               }
               else if (opcode == OpCodeType.OP_PUSHDATA4)
               {
                  if (pc + 3 >= rawScript.Length) { error = ScriptError.UNEXPECTED_END_OF_SCRIPT; return false; }
                  dataLength = (rawScript[pc + 3] << 24) | (rawScript[pc + 2] << 16) | (rawScript[pc + 1] << 8) | rawScript[pc]; // Little-endian uint
                  pc += 4;
               }

               if (dataLength == -1 && opcode != OpCodeType.OP_0) // Should not happen if logic is correct
               {
                  error = ScriptError.BAD_OPCODE; // Should be caught by dataLength checks or invalid opcode
                  return false;
               }


               if (dataLength > 0) // Only try to read data if length is positive
               {
                  if (pc + dataLength > rawScript.Length) { error = ScriptError.PUSHDATA_SIZE_ERROR; return false; } // Not enough data bytes in script
                  if (dataLength > MAX_SCRIPT_ELEMENT_SIZE) { error = ScriptError.DATA_TOO_LARGE; return false; }

                  dataToPush = new byte[dataLength];
                  Array.Copy(rawScript, pc, dataToPush, 0, dataLength);
                  pc += dataLength;
               }
               else if (dataLength == 0 && opcode != OpCodeType.OP_0) // e.g. OP_PUSHDATA1 with length 0
               {
                   dataToPush = System.Array.Empty<byte>();
               }
               // If dataLength was -1 due to OP_0, dataToPush is already set to empty array if opcode was OP_0

               // For direct pushes (0x01-0x4b), the opcode in ScriptElement is the original opcode (length).
               // For OP_PUSHDATA1/2/4, the opcode in ScriptElement is OP_PUSHDATA1/2/4.
               // For OP_0, the opcode is OP_0.
               parsedScriptElements.Add(new ScriptElement(opcode, dataToPush ?? System.Array.Empty<byte>()));
            }
            else // Non-data pushing opcodes (or OP_1NEGATE, OP_1-OP_16)
            {
               // Check if opcode is defined in OpCodeType enum.
               // This is a basic check; some defined opcodes might still be disabled or invalid in certain contexts.
               if (!Enum.IsDefined(typeof(OpCodeType), opcode))
               {
                  // This case is for truly undefined byte values not in OpCodeType enum.
                  // Bitcoin Core might treat some of these as OP_INVALIDOPCODE or just fail.
                  // For robustness, let's create an element representing the invalid opcode.
                  // However, our ScriptElement constructor for non-data opcodes might throw if it's not a known non-pushing opcode.
                  // A better approach for truly invalid opcodes:
                  error = ScriptError.BAD_OPCODE;
                  // Optionally, add a ScriptElement representing the failure point:
                  // parsedScriptElements.Add(new ScriptElement(OpCodeType.OP_INVALIDOPCODE, new byte[] { (byte)opcode }));
                  return false;
               }

               // OP_1NEGATE and OP_1 to OP_16 are single-byte opcodes that also push data.
               // ScriptElement constructor handles creating their data.
               parsedScriptElements.Add(new ScriptElement(opcode));
            }
         }

         // Ensure no trailing bytes after a PUSHDATA that claimed to read to end but didn't.
         // The loop condition pc < rawScript.Length and pc increments should handle this.
         // If pc == rawScript.Length, all bytes consumed. If pc > rawScript.Length, error was caught.

         error = ScriptError.UNKNOWN_ERROR; // No error if we reach here
         return true; // Success
      }
   }
}

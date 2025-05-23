using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics; // For BigInteger if ScriptNum uses it extensively or for intermediate calcs
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Chain.Bitcoin.Crypto; // For RIPEMD160 and Secp256k1ExternalLib
using MithrilShards.Core.Network.Protocol.Serialization; // For IProtocolTypeSerializer

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Script
{
   public class ScriptInterpreter
   {
      private const int MAX_SCRIPT_ELEMENT_SIZE = 520; // Max size of a script element (push data or number)
      private const int MAX_STACK_SIZE = 1000;       // Max number of items on the stack
      // MAX_SCRIPT_SIZE = 10000; (Consensus rule, usually checked before interpreter)
      // MAX_OPS_PER_SCRIPT = 201; (Consensus rule for non-segwit)

      // TODO: Inject IConsensusParameters for rules like MAX_OPS_PER_SCRIPT, script version flags etc.
      // For now, using constants where appropriate or deferring checks.

      private readonly IProtocolTypeSerializer<Transaction> _transactionSerializer;

      public ScriptInterpreter(IProtocolTypeSerializer<Transaction> transactionSerializer)
      {
         _transactionSerializer = transactionSerializer ?? throw new ArgumentNullException(nameof(transactionSerializer));
      }

      public bool Evaluate(ScriptEvaluationContext context)
      {
         if (context.Script.Count > MAX_SCRIPT_SIZE) // This check is usually on raw script bytes, not element count
         {
            // For now, assuming Script.Count is a proxy or this check is done elsewhere.
            // A proper check would be on the sum of element sizes if they were raw bytes.
            // context.SetError(ScriptError.SCRIPT_SIZE);
            // return false;
         }

         int opCount = 0;

         try
         {
            for (context.ProgramCounter = 0; context.ProgramCounter < context.Script.Count && !context.HaltExecution; context.ProgramCounter++)
            {
               ScriptElement element = context.Script[context.ProgramCounter];

               // Check if we are in a non-executing branch of an IF/ELSE/ENDIF block
               if (!context.IsBranchExecuting() && !(element.OpCode >= OpCodeType.OP_IF && element.OpCode <= OpCodeType.OP_ENDIF))
               {
                  continue; // Skip if not in an executing branch and opcode is not a conditional itself
               }

               // Check stack size limits before pushing, if the operation pushes.
               // Specific push operations will handle this more precisely.
               if (element.IsPushData() && (context.MainStack.Count + context.AltStack.Count) >= MAX_STACK_SIZE)
               {
                  context.SetError(ScriptError.STACK_SIZE_EXCEEDED); // STACK_SIZE in bitcoin core
                  return false;
               }


               if (element.IsPushData())
               {
                  if (element.Data == null) // Should not happen if ScriptElement constructor is robust
                  {
                     context.SetError(ScriptError.UNKNOWN_ERROR); // Or specific BAD_OPCODE if data is unexpectedly null
                     return false;
                  }
                  if (element.Data.Length > MAX_SCRIPT_ELEMENT_SIZE)
                  {
                     context.SetError(ScriptError.UNKNOWN_ERROR); // PUSHDATA_SIZE in bitcoin core
                     return false;
                  }
                  context.MainStack.Push(element.Data);
               }
               else // It's an operational opcode
               {
                  opCount++;
                  // TODO: if (!context.IsSegWit() && opCount > MAX_OPS_PER_SCRIPT) { context.SetError(...); return false; }

                  bool success = ExecuteOpCode(context, element.OpCode);
                  if (!success)
                  {
                     // context.ScriptFailed should already be true if ExecuteOpCode failed.
                     // context.HaltExecution might also be true.
                     return false;
                  }
               }

               // Check stack size after operation
               if ((context.MainStack.Count + context.AltStack.Count) > MAX_STACK_SIZE)
               {
                  context.SetError(ScriptError.STACK_SIZE_EXCEEDED);
                  return false;
               }
            } // End of script execution loop

            // If loop finished due to HaltExecution but not ScriptFailed (e.g. OP_RETURN), ScriptFailed should be true.
            if (context.HaltExecution && !context.ScriptFailed)
            {
               // This case implies an OP_RETURN that wasn't handled as an error by ExecuteOpCode itself.
               // OP_RETURN should set ScriptFailed.
               context.SetError(ScriptError.UNKNOWN_ERROR); // OP_RETURN_INVALID by Bitcoin Core
            }


            // If conditionals stack is not empty, it means an OP_IF was not matched with OP_ENDIF
            if (context.ConditionalsStack.Count != 0)
            {
               context.SetError(ScriptError.UNKNOWN_ERROR); // UNBALANCED_CONDITIONAL in bitcoin core
               return false;
            }

         }
         catch (Exception) // Catches issues from stack operations, etc.
         {
            context.SetError(ScriptError.UNKNOWN_ERROR); // Should be more specific if possible
            return false;
         }

         // After script execution, for the script to be valid (in many contexts like P2SH, P2WSH, or standard pubkey/scriptSig),
         // the main stack must not be empty, and the top item must evaluate to true.
         // This specific check is context-dependent and usually done by the caller of Evaluate.
         // For now, Evaluate just returns !context.ScriptFailed.
         return !context.ScriptFailed;
      }


      private bool ExecuteOpCode(ScriptEvaluationContext context, OpCodeType opcode)
      {
         // Stack Operations
         if (HandleStackOps(context, opcode)) return !context.ScriptFailed;

         // Arithmetic Operations
         if (HandleArithmeticOps(context, opcode)) return !context.ScriptFailed;

         // Control Flow Operations
         if (HandleControlFlowOps(context, opcode)) return !context.ScriptFailed;

         // Cryptographic Operations
         if (HandleCryptoOps(context, opcode)) return !context.ScriptFailed;

         // Splice Operations (Later)
         // Bitwise Logic Operations (Later)
         // Locktime Operations (Later)
         // Pseudo-words (Later)

         // Reserved Words (catch-all for remaining opcodes that might be disabled)
         if (IsDisabledReservedOpcode(opcode))
         {
            context.SetError(ScriptError.OP_DISABLED);
            return false;
         }


         // If opcode not handled by any specific group, it's an unknown or genuinely disabled opcode
         context.SetError(ScriptError.OP_DISABLED); // Or BAD_OPCODE / UNKNOWN_OPCODE
         return false;
      }

      private bool IsDisabledReservedOpcode(OpCodeType opcode)
      {
         // Consolidate checks for opcodes that are explicitly disabled or reserved.
         // This list can be expanded based on OpCodeType.cs definitions.
         return (opcode >= OpCodeType.OP_RESERVED && opcode <= OpCodeType.OP_VEROPNOTIF && opcode != OpCodeType.OP_NOP) || // Standard reserved/disabled range, excluding NOP if it's there
                opcode == OpCodeType.OP_CAT || opcode == OpCodeType.OP_SUBSTR ||
                opcode == OpCodeType.OP_LEFT || opcode == OpCodeType.OP_RIGHT ||
                opcode == OpCodeType.OP_INVERT || opcode == OpCodeType.OP_AND ||
                opcode == OpCodeType.OP_OR || opcode == OpCodeType.OP_XOR ||
                opcode == OpCodeType.OP_2MUL || opcode == OpCodeType.OP_2DIV ||
                opcode == OpCodeType.OP_MUL || opcode == OpCodeType.OP_DIV ||
                opcode == OpCodeType.OP_MOD || opcode == OpCodeType.OP_LSHIFT ||
                opcode == OpCodeType.OP_RSHIFT ||
                opcode == OpCodeType.OP_RESERVED1 || opcode == OpCodeType.OP_RESERVED2;
      }

      private bool HandleStackOps(ScriptEvaluationContext context, OpCodeType opcode)
      {
         switch (opcode)
         {
            // OP_0/OP_FALSE and OP_1-OP_16 are handled by IsPushData() in the main loop.
            // OP_PUSHDATA1,2,4 are also handled by IsPushData() as ScriptElement would have data.

            case OpCodeType.OP_TOALTSTACK:
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               context.AltStack.Push(context.MainStack.Pop());
               return true;

            case OpCodeType.OP_FROMALTSTACK:
               if (context.AltStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               context.MainStack.Push(context.AltStack.Pop());
               return true;

            case OpCodeType.OP_IFDUP:
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               if (CastToBool(context.MainStack.Peek()))
               {
                  context.MainStack.Push(context.MainStack.Peek()); // Push a copy
               }
               return true;

            case OpCodeType.OP_DEPTH:
               context.MainStack.Push(new ScriptNum(context.MainStack.Count).ToBytes());
               return true;

            case OpCodeType.OP_DROP:
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               context.MainStack.Pop();
               return true;

            case OpCodeType.OP_DUP:
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               context.MainStack.Push(context.MainStack.Peek());
               return true;

            case OpCodeType.OP_NIP:
               if (context.MainStack.Count < 2) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var valNip = context.MainStack.Pop(); // x2
               context.MainStack.Pop();             // x1 (discarded)
               context.MainStack.Push(valNip);      // push x2 back
               return true;

            case OpCodeType.OP_OVER:
               if (context.MainStack.Count < 2) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var overVal2 = context.MainStack.ElementAt(1); // Second from top (peek)
               context.MainStack.Push(overVal2);
               return true;

            case OpCodeType.OP_PICK:
            case OpCodeType.OP_ROLL:
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var numPickRoll = new ScriptNum(context.MainStack.Pop(), true, ScriptNum.MAXIMUM_ELEMENT_SIZE).IntValue; // CastToScriptNum(context.MainStack.Pop(), true).ToInt32Checked();
               if (numPickRoll < 0 || numPickRoll >= context.MainStack.Count) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var itemPickRoll = context.MainStack.ElementAt(numPickRoll);
               if (opcode == OpCodeType.OP_ROLL)
               {
                  // To "roll", we need to remove it from its current position and push to top.
                  // This is inefficient with Stack<T>.ToList().RemoveAt().
                  // A temporary list might be better.
                  var tempList = context.MainStack.ToList();
                  var itemToMove = tempList[tempList.Count - 1 - numPickRoll]; // ElementAt is 0-indexed from top, List is 0-indexed from bottom
                  tempList.RemoveAt(tempList.Count - 1 - numPickRoll);
                  context.MainStack.Clear();
                  for(int i = tempList.Count -1; i >=0; i--) context.MainStack.Push(tempList[i]); // Rebuild stack
                  context.MainStack.Push(itemToMove);
               }
               else // OP_PICK
               {
                  context.MainStack.Push(itemPickRoll);
               }
               return true;

            case OpCodeType.OP_ROT:
               if (context.MainStack.Count < 3) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var rotVal3 = context.MainStack.Pop(); // x3
               var rotVal2 = context.MainStack.Pop(); // x2
               var rotVal1 = context.MainStack.Pop(); // x1
               context.MainStack.Push(rotVal2); // x2 on top
               context.MainStack.Push(rotVal3); // x3
               context.MainStack.Push(rotVal1); // x1
               return true;

            case OpCodeType.OP_SWAP:
               if (context.MainStack.Count < 2) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var swapVal2 = context.MainStack.Pop(); // x2
               var swapVal1 = context.MainStack.Pop(); // x1
               context.MainStack.Push(swapVal2); // x2 on top
               context.MainStack.Push(swapVal1); // x1
               return true;

            case OpCodeType.OP_TUCK: // x1 x2 -> x2 x1 x2
               if (context.MainStack.Count < 2) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var tuckVal2 = context.MainStack.Pop(); // x2
               var tuckVal1 = context.MainStack.Pop(); // x1
               context.MainStack.Push(tuckVal2); // x2
               context.MainStack.Push(tuckVal1); // x1
               context.MainStack.Push(tuckVal2); // x2 again
               return true;

            case OpCodeType.OP_2DROP: // x1 x2 ->
               if (context.MainStack.Count < 2) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               context.MainStack.Pop();
               context.MainStack.Pop();
               return true;

            case OpCodeType.OP_2DUP: // x1 x2 -> x1 x2 x1 x2
               if (context.MainStack.Count < 2) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var dup2_x2 = context.MainStack.Peek();
               var dup2_x1 = context.MainStack.ElementAt(1);
               context.MainStack.Push(dup2_x1);
               context.MainStack.Push(dup2_x2);
               return true;

            case OpCodeType.OP_3DUP: // x1 x2 x3 -> x1 x2 x3 x1 x2 x3
               if (context.MainStack.Count < 3) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var dup3_x3 = context.MainStack.Peek();
               var dup3_x2 = context.MainStack.ElementAt(1);
               var dup3_x1 = context.MainStack.ElementAt(2);
               context.MainStack.Push(dup3_x1);
               context.MainStack.Push(dup3_x2);
               context.MainStack.Push(dup3_x3);
               return true;

            case OpCodeType.OP_2OVER: // x1 x2 x3 x4 -> x1 x2 x3 x4 x1 x2
               if (context.MainStack.Count < 4) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var over2_x2 = context.MainStack.ElementAt(2); // x2 (3rd from top)
               var over2_x1 = context.MainStack.ElementAt(3); // x1 (4th from top)
               context.MainStack.Push(over2_x1);
               context.MainStack.Push(over2_x2);
               return true;

            case OpCodeType.OP_2ROT: // x1 x2 x3 x4 x5 x6 -> x3 x4 x5 x6 x1 x2
               if (context.MainStack.Count < 6) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var rot2_x6 = context.MainStack.Pop();
               var rot2_x5 = context.MainStack.Pop();
               var rot2_x4 = context.MainStack.Pop();
               var rot2_x3 = context.MainStack.Pop();
               var rot2_x2 = context.MainStack.Pop();
               var rot2_x1 = context.MainStack.Pop();
               context.MainStack.Push(rot2_x3);
               context.MainStack.Push(rot2_x4);
               context.MainStack.Push(rot2_x5);
               context.MainStack.Push(rot2_x6);
               context.MainStack.Push(rot2_x1);
               context.MainStack.Push(rot2_x2);
               return true;

            case OpCodeType.OP_2SWAP: // x1 x2 x3 x4 -> x3 x4 x1 x2
               if (context.MainStack.Count < 4) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var swap2_x4 = context.MainStack.Pop();
               var swap2_x3 = context.MainStack.Pop();
               var swap2_x2 = context.MainStack.Pop();
               var swap2_x1 = context.MainStack.Pop();
               context.MainStack.Push(swap2_x3);
               context.MainStack.Push(swap2_x4);
               context.MainStack.Push(swap2_x1);
               context.MainStack.Push(swap2_x2);
               return true;

            default:
               return false; // Opcode not handled by this group
         }
      }

      private bool HandleArithmeticOps(ScriptEvaluationContext context, OpCodeType opcode)
      {
         ScriptNum sn1, sn2, result;
         switch (opcode)
         {
            case OpCodeType.OP_1ADD: // x -> x+1
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               sn1 = new ScriptNum(context.MainStack.Pop(), true);
               result = sn1 + new ScriptNum(1);
               context.MainStack.Push(result.ToBytes());
               return true;

            case OpCodeType.OP_1SUB: // x -> x-1
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               sn1 = new ScriptNum(context.MainStack.Pop(), true);
               result = sn1 - new ScriptNum(1);
               context.MainStack.Push(result.ToBytes());
               return true;

            case OpCodeType.OP_NEGATE: // x -> -x
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               sn1 = new ScriptNum(context.MainStack.Pop(), true);
               result = -sn1;
               context.MainStack.Push(result.ToBytes());
               return true;

            case OpCodeType.OP_ABS: // x -> |x|
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               sn1 = new ScriptNum(context.MainStack.Pop(), true);
               if (sn1 < 0) result = -sn1; else result = sn1;
               context.MainStack.Push(result.ToBytes());
               return true;

            case OpCodeType.OP_NOT: // x -> (x==0 ? 1 : 0)
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               sn1 = new ScriptNum(context.MainStack.Pop(), true);
               result = new ScriptNum(sn1 == 0 ? 1 : 0);
               context.MainStack.Push(result.ToBytes());
               return true;

            case OpCodeType.OP_0NOTEQUAL: // x -> (x==0 ? 0 : 1)
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               sn1 = new ScriptNum(context.MainStack.Pop(), true);
               result = new ScriptNum(sn1 == 0 ? 0 : 1);
               context.MainStack.Push(result.ToBytes());
               return true;

            case OpCodeType.OP_ADD: // x1 x2 -> x1+x2
            case OpCodeType.OP_SUB: // x1 x2 -> x1-x2
            case OpCodeType.OP_BOOLAND: // x1 x2 -> (x1!=0 && x2!=0 ? 1 : 0)
            case OpCodeType.OP_BOOLOR:  // x1 x2 -> (x1!=0 || x2!=0 ? 1 : 0)
            case OpCodeType.OP_NUMEQUAL: // x1 x2 -> (x1==x2 ? 1 : 0)
            case OpCodeType.OP_NUMNOTEQUAL: // x1 x2 -> (x1==x2 ? 0 : 1)
            case OpCodeType.OP_LESSTHAN: // x1 x2 -> (x1<x2 ? 1 : 0)
            case OpCodeType.OP_GREATERTHAN: // x1 x2 -> (x1>x2 ? 1 : 0)
            case OpCodeType.OP_LESSTHANOREQUAL: // x1 x2 -> (x1<=x2 ? 1 : 0)
            case OpCodeType.OP_GREATERTHANOREQUAL: // x1 x2 -> (x1>=x2 ? 1 : 0)
            case OpCodeType.OP_MIN: // x1 x2 -> min(x1,x2)
            case OpCodeType.OP_MAX: // x1 x2 -> max(x1,x2)
               if (context.MainStack.Count < 2) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               sn2 = new ScriptNum(context.MainStack.Pop(), true); // item on top is second operand
               sn1 = new ScriptNum(context.MainStack.Pop(), true); // item below is first operand

               switch (opcode)
               {
                  case OpCodeType.OP_ADD: result = sn1 + sn2; break;
                  case OpCodeType.OP_SUB: result = sn1 - sn2; break;
                  case OpCodeType.OP_BOOLAND: result = new ScriptNum((sn1 != 0 && sn2 != 0) ? 1 : 0); break;
                  case OpCodeType.OP_BOOLOR: result = new ScriptNum((sn1 != 0 || sn2 != 0) ? 1 : 0); break;
                  case OpCodeType.OP_NUMEQUAL: result = new ScriptNum((sn1 == sn2) ? 1 : 0); break;
                  case OpCodeType.OP_NUMNOTEQUAL: result = new ScriptNum((sn1 == sn2) ? 0 : 1); break;
                  case OpCodeType.OP_LESSTHAN: result = new ScriptNum((sn1 < sn2) ? 1 : 0); break;
                  case OpCodeType.OP_GREATERTHAN: result = new ScriptNum((sn1 > sn2) ? 1 : 0); break;
                  case OpCodeType.OP_LESSTHANOREQUAL: result = new ScriptNum((sn1 <= sn2) ? 1 : 0); break;
                  case OpCodeType.OP_GREATERTHANOREQUAL: result = new ScriptNum((sn1 >= sn2) ? 1 : 0); break;
                  case OpCodeType.OP_MIN: result = (sn1 < sn2) ? sn1 : sn2; break;
                  case OpCodeType.OP_MAX: result = (sn1 > sn2) ? sn1 : sn2; break;
                  default: throw new InvalidOperationException("Opcode fallthrough in arithmetic"); // Should not happen
               }
               context.MainStack.Push(result.ToBytes());
               return true;

            case OpCodeType.OP_NUMEQUALVERIFY: // x1 x2 -> fail if not equal
               if (context.MainStack.Count < 2) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               sn2 = new ScriptNum(context.MainStack.Pop(), true);
               sn1 = new ScriptNum(context.MainStack.Pop(), true);
               if (sn1 != sn2) { context.SetError(ScriptError.VERIFY_FAILED); return false; } // NUMEQUALVERIFY_FAILED
               return true;

            case OpCodeType.OP_WITHIN: // x min max -> (x >= min && x < max ? 1 : 0)
               if (context.MainStack.Count < 3) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var maxVal = new ScriptNum(context.MainStack.Pop(), true);
               var minVal = new ScriptNum(context.MainStack.Pop(), true);
               var valX = new ScriptNum(context.MainStack.Pop(), true);
               result = new ScriptNum((valX >= minVal && valX < maxVal) ? 1 : 0);
               context.MainStack.Push(result.ToBytes());
               return true;

            default:
               return false; // Opcode not handled by this group
         }
      }

      private bool HandleControlFlowOps(ScriptEvaluationContext context, OpCodeType opcode)
      {
         switch (opcode)
         {
            case OpCodeType.OP_IF:
            case OpCodeType.OP_NOTIF:
               bool condition = false;
               if (context.IsBranchExecuting()) // Only evaluate condition if we are in an executing branch
               {
                  if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; } // UNBALANCED_CONDITIONAL or INVALID_STACK_OPERATION
                  var top = context.MainStack.Pop();
                  condition = CastToBool(top);
                  if (opcode == OpCodeType.OP_NOTIF)
                  {
                     condition = !condition;
                  }
               }
               context.ConditionalsStack.Push(condition);
               return true;

            case OpCodeType.OP_ELSE:
               if (context.ConditionalsStack.Count == 0) { context.SetError(ScriptError.UNKNOWN_ERROR); return false; } // UNBALANCED_CONDITIONAL
               // Invert the top of the conditionals stack (current branch condition)
               context.ConditionalsStack.Push(!context.ConditionalsStack.Pop());
               return true;

            case OpCodeType.OP_ENDIF:
               if (context.ConditionalsStack.Count == 0) { context.SetError(ScriptError.UNKNOWN_ERROR); return false; } // UNBALANCED_CONDITIONAL
               context.ConditionalsStack.Pop();
               return true;

            case OpCodeType.OP_VERIFY:
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               if (!CastToBool(context.MainStack.Pop()))
               {
                  context.SetError(ScriptError.VERIFY_FAILED); // VERIFY
                  return false;
               }
               return true;

            case OpCodeType.OP_RETURN:
               context.SetError(ScriptError.UNKNOWN_ERROR); // OP_RETURN by Bitcoin Core
               return false; // This will set ScriptFailed and HaltExecution

            default:
               return false; // Opcode not handled by this group
         }
      }

      /// <summary>
      /// Casts a script stack item (byte array) to a boolean.
      /// Bitcoin script rule: An empty vector is false. 0x80 is false (-0). All others are true.
      /// This also needs to consider minimal encoding for P2SH/SegWit.
      /// </summary>
      public static bool CastToBool(byte[] data)
      {
         // Rule: Empty vector is false. 0x80 is false (-0). All others are true.
         // This needs to be precise for script evaluation.
         // For P2SH/SegWit, minimal encoding rules apply to numbers, but CastToBool itself is simpler.
         if (data == null || data.Length == 0) return false;

         for (int i = 0; i < data.Length; i++)
         {
            if (data[i] != 0)
            {
               // Special case: 0x80 is negative zero, which is false.
               if (i == data.Length - 1 && data[i] == 0x80)
                  return false;
               return true; // Any other non-zero value is true.
            }
         }
         return false; // All bytes are zero, so it's false.
      }


      private bool HandleCryptoOps(ScriptEvaluationContext context, OpCodeType opcode)
      {
         byte[] data1;
         byte[] hash;

         switch (opcode)
         {
            case OpCodeType.OP_RIPEMD160:
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               data1 = context.MainStack.Pop();
               hash = RIPEMD160.ComputeHash(data1); // Use the (stubbed) RIPEMD160 class
               context.MainStack.Push(hash);
               return true;

            case OpCodeType.OP_SHA1:
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               data1 = context.MainStack.Pop();
               using (var sha1 = System.Security.Cryptography.SHA1.Create())
               {
                  hash = sha1.ComputeHash(data1);
               }
               context.MainStack.Push(hash);
               return true;

            case OpCodeType.OP_SHA256:
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               data1 = context.MainStack.Pop();
               using (var sha256 = System.Security.Cryptography.SHA256.Create())
               {
                  hash = sha256.ComputeHash(data1);
               }
               context.MainStack.Push(hash);
               return true;

            case OpCodeType.OP_HASH160: // SHA256 then RIPEMD160
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               data1 = context.MainStack.Pop();
               byte[] sha256Hash;
               using (var sha256 = System.Security.Cryptography.SHA256.Create())
               {
                  sha256Hash = sha256.ComputeHash(data1);
               }
               hash = RIPEMD160.ComputeHash(sha256Hash); // Use the (stubbed) RIPEMD160 class
               context.MainStack.Push(hash);
               return true;

            case OpCodeType.OP_HASH256: // SHA256 twice
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               data1 = context.MainStack.Pop();
               byte[] firstSha256;
               using (var sha256 = System.Security.Cryptography.SHA256.Create())
               {
                  firstSha256 = sha256.ComputeHash(data1);
                  hash = sha256.ComputeHash(firstSha256);
               }
               context.MainStack.Push(hash);
               return true;

            case OpCodeType.OP_CODESEPARATOR:
               context.CodeSeparatorPosition = context.ProgramCounter + 1;
               return true;

            case OpCodeType.OP_CHECKSIG:
            case OpCodeType.OP_CHECKSIGVERIFY:
               if (context.MainStack.Count < 2) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               byte[] pubKeyCS = context.MainStack.Pop();
               byte[] sigCS = context.MainStack.Pop();

               // TODO: Actual sighash calculation will depend on context.Transaction, context.InputIndex,
               // scriptCode (derived from the script and context.CodeSeparatorPosition), and sighash type byte from sigCS.
               // For now, we create a dummy sighash or acknowledge it's missing for the stubbed verification.
               byte[] subScriptForSig = GetSubScriptForSignature(context); // Get the scriptCode
               bool sigValid = CheckECDSASignature(sigCS, pubKeyCS, subScriptForSig, context);

               context.MainStack.Push(ScriptNum.FromBool(sigValid).ToBytes());

               if (opcode == OpCodeType.OP_CHECKSIGVERIFY)
               {
                  if (!sigValid) { context.SetError(ScriptError.VERIFY_FAILED); return false; } // CHECKSIGVERIFY
                  context.MainStack.Pop(); // Pop the true result from stack
               }
               return true;

            case OpCodeType.OP_CHECKMULTISIG:
            case OpCodeType.OP_CHECKMULTISIGVERIFY:
               // Pop n (number of public keys)
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               int nKeys = (int)new ScriptNum(context.MainStack.Pop(), true, ScriptNum.MAXIMUM_ELEMENT_SIZE).Value; // Max keys limited by script element size for n
               if (nKeys < 0 || nKeys > 20) { context.SetError(ScriptError.UNKNOWN_ERROR); return false; } // OP_CHECKMULTISIG limits n to 20 (PUBKEY_COUNT_OUT_OF_RANGE)
               // opCount += nKeys; // TODO: if (opCount > MAX_OPS_PER_SCRIPT_AFTER_SEGWIT) fail; (or before segwit MAX_OPS_PER_SCRIPT)

               if (context.MainStack.Count < nKeys) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var pubKeysCMS = new List<byte[]>(nKeys);
               for (int k = 0; k < nKeys; k++) pubKeysCMS.Add(context.MainStack.Pop());

               // Pop m (number of required signatures)
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               int mSigs = (int)new ScriptNum(context.MainStack.Pop(), true, ScriptNum.MAXIMUM_ELEMENT_SIZE).Value;
               if (mSigs < 0 || mSigs > nKeys) { context.SetError(ScriptError.UNKNOWN_ERROR); return false; } // SIG_COUNT_OUT_OF_RANGE (or m > n)

               if (context.MainStack.Count < mSigs) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; }
               var sigsCMS = new List<byte[]>(mSigs);
               for (int k = 0; k < mSigs; k++) sigsCMS.Add(context.MainStack.Pop());

               // Pop dummy element due to Bitcoin Core bug
               if (context.MainStack.Count < 1) { context.SetError(ScriptError.INVALID_STACK_OPERATION); return false; } // DUMMY_ELEMENT_MISSING
               context.MainStack.Pop(); // Dummy value (not used)

               byte[] subScriptForMultiSig = GetSubScriptForSignature(context);
               int sigOkCount = 0;
               int sigIdx = 0;
               int keyIdx = 0;

               // Loop through signatures and public keys
               // This is a simplified matching. Real version has more complex iteration due to potential signature order.
               // Bitcoin Core: iterates signatures first, then public keys until a match is found or keys run out.
               while (sigOkCount < mSigs && sigIdx < mSigs && keyIdx < nKeys)
               {
                  if (CheckECDSASignature(sigsCMS[sigIdx], pubKeysCMS[keyIdx], subScriptForMultiSig, context))
                  {
                     sigOkCount++;
                     sigIdx++; // Consume this signature
                  }
                  keyIdx++; // Always advance key index, try next pubkey for current sig if this one failed,
                            // or next pubkey for next sig if this one succeeded.
                  // If too many keys are checked for current sig without success:
                  if (nKeys - keyIdx < mSigs - sigOkCount) break; // Not enough keys left to satisfy remaining sigs
               }

               context.MainStack.Push(ScriptNum.FromBool(sigOkCount == mSigs).ToBytes());

               if (opcode == OpCodeType.OP_CHECKMULTISIGVERIFY)
               {
                  if (sigOkCount != mSigs) { context.SetError(ScriptError.VERIFY_FAILED); return false; } // CHECKMULTISIGVERIFY
                  context.MainStack.Pop(); // Pop the true result
               }
               return true;

            default:
               return false; // Opcode not handled by this group
         }
      }

      /// <summary>
      /// Performs ECDSA signature verification.
      /// </summary>
      private bool CheckECDSASignature(byte[] rawSignatureFromStack, byte[] rawPubKeyFromStack, byte[] subScriptForSighash, ScriptEvaluationContext context)
      {
         if (context.Transaction == null)
         {
            // This should not happen if context is properly populated for CHECKSIG operations.
            System.Diagnostics.Debug.WriteLine("Error: Transaction context not available for signature verification.");
            context.SetError(ScriptError.UNKNOWN_ERROR); // Or a more specific error like CONTEXT_MISSING_TX
            return false;
         }

         if (rawSignatureFromStack == null || rawSignatureFromStack.Length == 0)
         {
            // Empty signature is considered invalid by CHECKSIG (but can be valid placeholder in CHECKMULTISIG dummy)
            // However, if it reaches here in CHECKSIG, it's an actual signature to verify.
            return false; // Invalid signature format
         }

         if (rawPubKeyFromStack == null || rawPubKeyFromStack.Length == 0)
         {
            return false; // Invalid public key format
         }

         // Extract sighash type and DER-encoded signature
         byte sighashTypeByte = rawSignatureFromStack.Last();
         byte[] derSignature = rawSignatureFromStack.Take(rawSignatureFromStack.Length - 1).ToArray();

         // TODO: Add script flags checks (e.g., SCRIPT_VERIFY_STRICTENC, SCRIPT_VERIFY_LOW_S, SCRIPT_VERIFY_NULLFAIL)

         // 1. Calculate Sighash
         byte[]? sighashToVerify;
         if (context.Flags.HasFlag(ScriptFlags.WitnessV0))
         {
            // For SegWit v0, use BIP143 sighash
            // subScriptForSighash is the scriptCode (e.g., witnessScript for P2WSH, or derived P2WPKH script)
            sighashToVerify = SighashGenerator.CalculateWitnessSignatureHash(
                context.Transaction,
                context.InputIndex,
                subScriptForSighash, // This is the scriptCode
                context.Amount,      // Amount of the UTXO being spent
                sighashTypeByte
            );
         }
         else
         {
            // For legacy, use the legacy sighash algorithm
            sighashToVerify = SighashGenerator.CalculateLegacySignatureHash(
                context.Transaction,
                context.InputIndex,
                subScriptForSighash, // This is the scriptCode (subScript)
                sighashTypeByte,
                _transactionSerializer
            );
         }

         if (sighashToVerify == null)
         {
            // Sighash calculation failed (e.g., invalid input index)
            System.Diagnostics.Debug.WriteLine("Sighash calculation returned null.");
            // Bitcoin Core might return false from CheckSig without setting a script error here.
            // Let's ensure verification fails.
            return false;
         }

         // 2. Verify Signature using the chosen (stubbed) library
         // We use Secp256k1BouncyCastle for actual verification now.
         bool isValid = Secp256k1BouncyCastle.VerifySignature(sighashToVerify, derSignature, rawPubKeyFromStack);

         // TODO: Implement SCRIPT_VERIFY_NULLFAIL: if signature check fails and signature is not empty,
         // the whole script must fail, not just return false for CHECKSIG.
         // if (!isValid && derSignature.Length > 0 && (context.Flags & ScriptFlags.VERIFY_NULLFAIL) != 0) {
         //    context.SetError(ScriptError.SIG_NULLFAIL);
         //    return false;
         // }

         return isValid;
      }

      /// <summary>
      /// Helper to get the sub-script (scriptCode) for signature hashing.
      /// This needs to be correctly implemented according to Bitcoin rules,
      /// removing OP_CODESEPARATORs and potentially signatures from the script.
      /// </summary>
      private byte[] GetSubScriptForSignature(ScriptEvaluationContext context)
      {
         var rawScript = new List<byte>();
         // This is a simplification. Real scriptCode removes OP_CODESEPARATOR and everything before the last one.
         // And for non-segwit, it also removes signatures from the script.
         // For now, just use the part of the script after the last OP_CODESEPARATOR.
         for (int i = context.CodeSeparatorPosition; i < context.Script.Count; i++)
         {
            var element = context.Script[i];
            if (element.OpCode >= 0 && (int)element.OpCode <= (int)OpCodeType.OP_PUSHDATA4) // Push operation
            {
               if (element.OpCode < OpCodeType.OP_PUSHDATA1) // Direct push (length of data is the opcode)
               {
                  rawScript.Add((byte)element.OpCode);
               }
               else if (element.OpCode == OpCodeType.OP_PUSHDATA1)
               {
                  rawScript.Add((byte)OpCodeType.OP_PUSHDATA1);
                  rawScript.Add((byte)element.Data!.Length);
               }
               else if (element.OpCode == OpCodeType.OP_PUSHDATA2)
               {
                  rawScript.Add((byte)OpCodeType.OP_PUSHDATA2);
                  rawScript.AddRange(BitConverter.GetBytes((ushort)element.Data!.Length));
               }
               else // OP_PUSHDATA4
               {
                  rawScript.Add((byte)OpCodeType.OP_PUSHDATA4);
                  rawScript.AddRange(BitConverter.GetBytes((uint)element.Data!.Length));
               }
               if (element.Data != null) rawScript.AddRange(element.Data);
            }
            else
            {
               rawScript.Add((byte)element.OpCode);
            }
         }
         System.Diagnostics.Debug.WriteLine($"Warning: GetSubScriptForSignature is STUBBED and simplified.");
         return rawScript.ToArray();
      }
   }
}

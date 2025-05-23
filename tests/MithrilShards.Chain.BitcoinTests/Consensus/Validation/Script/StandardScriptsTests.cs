using System.Collections.Generic;
using System.Linq;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Script;
using MithrilShards.Chain.Bitcoin.Protocol.Types; // For OpCodeType
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Consensus.Validation.Script
{
   public class StandardScriptsTests
   {
      private List<ScriptElement> ScriptFromHex(string hex)
      {
         // This is a simplified parser for test purposes.
         // A real test might use ScriptParser if it's robust enough for simple hex strings.
         // For now, we'll manually create ScriptElements based on common patterns.
         // This helper would need to be more robust for complex scripts.
         // Example: "5120{32_byte_hex}" -> OP_1 PUSH_32 <data>

         var elements = new List<ScriptElement>();
         int i = 0;
         while (i < hex.Length)
         {
            byte opcodeVal = byte.Parse(hex.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
            i += 2;
            var opcode = (OpCodeType)opcodeVal;

            if (opcode >= OpCodeType.OP_1 && opcode <= OpCodeType.OP_16) // OP_N
            {
               elements.Add(new ScriptElement(opcode));
            }
            else if (opcode == OpCodeType.OP_0)
            {
               elements.Add(new ScriptElement(OpCodeType.OP_0));
            }
            else if (opcode >= (OpCodeType)0x01 && opcode <= (OpCodeType)0x4b) // Direct push (1-75 bytes)
            {
               int len = opcodeVal;
               if (i + len * 2 > hex.Length) throw new System.ArgumentException("Hex string too short for data push");
               byte[] data = Enumerable.Range(0, len)
                                   .Select(x => byte.Parse(hex.Substring(i + x * 2, 2), System.Globalization.NumberStyles.HexNumber))
                                   .ToArray();
               elements.Add(new ScriptElement(opcode, data));
               i += len * 2;
            }
            // Add other opcodes as needed for tests, e.g., OP_CHECKSIG, OP_HASH160 etc.
            else
            {
                 elements.Add(new ScriptElement(opcode));
            }
         }
         return elements;
      }

      // --- Tests for IsPayToTaproot (P2TR) ---
      [Fact]
      public void IsPayToTaproot_ValidScript_ReturnsTrue()
      {
         // OP_1 <32-byte-key>
         var scriptElements = new List<ScriptElement>
            {
                new ScriptElement(OpCodeType.OP_1),
                new ScriptElement((OpCodeType)0x20, new byte[32]) // PUSH_32 <32_bytes>
            };
         bool result = StandardScripts.IsPayToTaproot(scriptElements, out byte[]? outputKey);
         Assert.True(result);
         Assert.NotNull(outputKey);
         Assert.Equal(32, outputKey!.Length);
      }

      [Theory]
      [InlineData("0020" + "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f")] // OP_0 <32-bytes>
      [InlineData("51")] // OP_1 (too short)
      [InlineData("5120" + "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e" + "ac")] // OP_1 <31-bytes> OP_CHECKSIG (too many elements)
      [InlineData("511f" + "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e")]   // OP_1 <31-bytes>
      [InlineData("5121" + "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f00")] // OP_1 <33-bytes>
      public void IsPayToTaproot_InvalidScripts_ReturnsFalse(string scriptHex)
      {
         // A proper hex parser to ScriptElement list would be better here.
         // For now, these test cases are conceptual or would need manual ScriptElement list construction.
         // The IsPayToTaproot directly takes IList<ScriptElement>
         // Let's test the conditions directly:
         var elements = new List<ScriptElement>();
         if (scriptHex.StartsWith("00")) elements.Add(new ScriptElement(OpCodeType.OP_0));
         else if (scriptHex.StartsWith("51")) elements.Add(new ScriptElement(OpCodeType.OP_1));

         if (scriptHex.Length > 2) {
             if (scriptHex.Substring(2,2) == "20" && scriptHex.Length == 2 + 2 + 64)
                 elements.Add(new ScriptElement((OpCodeType)0x20, new byte[32]));
             else if (scriptHex.Substring(2,2) == "1f" && scriptHex.Length == 2 + 2 + 62)
                 elements.Add(new ScriptElement((OpCodeType)0x1f, new byte[31]));
             else if (scriptHex.Substring(2,2) == "21" && scriptHex.Length == 2 + 2 + 66)
                 elements.Add(new ScriptElement((OpCodeType)0x21, new byte[33]));
         }
         if (scriptHex.EndsWith("ac")) elements.Add(new ScriptElement(OpCodeType.OP_CHECKSIG));


         bool result = StandardScripts.IsPayToTaproot(elements, out byte[]? outputKey);
         Assert.False(result);
         Assert.Null(outputKey);
      }


      // --- Tests for other standard scripts ---
      // (P2PK, P2PKH, P2SH, P2WPKH, P2WSH, P2MS)
      // These should be expanded with more positive and negative cases.

      [Fact]
      public void IsPayToWitnessPubKeyHash_ValidScript_ReturnsTrue()
      {
         // OP_0 <20-byte-hash>
         var scriptElements = new List<ScriptElement>
            {
                new ScriptElement(OpCodeType.OP_0),
                new ScriptElement((OpCodeType)0x14, new byte[20]) // PUSH_20 <20_bytes>
            };
         bool result = StandardScripts.IsPayToWitnessPubKeyHash(scriptElements, out byte[]? pkh);
         Assert.True(result);
         Assert.NotNull(pkh);
         Assert.Equal(20, pkh!.Length);
      }

      [Fact]
      public void IsPayToWitnessScriptHash_ValidScript_ReturnsTrue()
      {
         // OP_0 <32-byte-hash>
         var scriptElements = new List<ScriptElement>
            {
                new ScriptElement(OpCodeType.OP_0),
                new ScriptElement((OpCodeType)0x20, new byte[32]) // PUSH_32 <32_bytes>
            };
         bool result = StandardScripts.IsPayToWitnessScriptHash(scriptElements, out byte[]? sh);
         Assert.True(result);
         Assert.NotNull(sh);
         Assert.Equal(32, sh!.Length);
      }

      // TODO: Add more positive and negative test cases for all Is... methods in StandardScripts.cs
      // e.g. IsPayToPubKey, IsPayToPubKeyHash, IsPayToScriptHash, IsPayToMultiSig.
      // For IsPayToMultiSig, test different m and n values, and pubkey counts.
      // For IsPushOnly, test various scripts.
   }
}

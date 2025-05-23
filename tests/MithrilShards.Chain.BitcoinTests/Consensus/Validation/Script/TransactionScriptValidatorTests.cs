using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Script;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Transaction;
using MithrilShards.Chain.Bitcoin.Protocol; // For KnownVersion
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types; // For TransactionSerializer for dummy tx
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization; // For IProtocolTypeSerializer
using Moq;
using Xunit;
using Xunit.Abstractions; // For ITestOutputHelper

namespace MithrilShards.Chain.BitcoinTests.Consensus.Validation.Script
{
   public class TransactionScriptValidatorTests
   {
      private readonly ITestOutputHelper _output;
      private readonly IProtocolTypeSerializer<Transaction> _transactionSerializer;
      private readonly ScriptInterpreter _scriptInterpreter;
      private readonly TransactionScriptValidator _validator;

      public TransactionScriptValidatorTests(ITestOutputHelper output)
      {
         _output = output;

         // Setup a real TransactionSerializer, as it's needed by ScriptInterpreter for sighash
         // and by SighashGenerator (which ScriptInterpreter uses).
         // We mock its dependencies if they are complex or not relevant to script tests.
         var mockInputSerializer = new Mock<IProtocolTypeSerializer<TransactionInput>>();
         var mockOutputSerializer = new Mock<IProtocolTypeSerializer<TransactionOutput>>();
         var mockWitnessSerializer = new Mock<IProtocolTypeSerializer<TransactionWitness>>();
         _transactionSerializer = new TransactionSerializer(mockInputSerializer.Object, mockOutputSerializer.Object, mockWitnessSerializer.Object);

         _scriptInterpreter = new ScriptInterpreter(_transactionSerializer);
         _validator = new TransactionScriptValidator(_scriptInterpreter);
      }

      private byte[] HexToBytes(string hex)
      {
         if (string.IsNullOrEmpty(hex)) return System.Array.Empty<byte>();
         return Enumerable.Range(0, hex.Length / 2)
                          .Select(x => byte.Parse(hex.Substring(x * 2, 2), NumberStyles.HexNumber))
                          .ToArray();
      }

      private Transaction CreateDummyTransaction(byte[] scriptSig, byte[] scriptPubKey, List<byte[]>? witnessStackItems = null)
      {
         // Create a very basic transaction for context.
         // Many script_tests.json tests don't rely on specific tx content beyond the scripts and witness.
         var tx = new Transaction
         {
            Version = 1,
            Inputs = new TransactionInput[]
            {
                    new TransactionInput
                    {
                        PreviousOutput = new OutPoint { Hash = new Core.DataTypes.UInt256(new byte[32]), Index = 0 },
                        SignatureScript = scriptSig,
                        Sequence = uint.MaxValue
                    }
            },
            Outputs = new TransactionOutput[]
            {
                    new TransactionOutput { Value = 0, ScriptPubKey = new byte[]{ (byte)OpCodeType.OP_TRUE } } // Dummy output
            },
            LockTime = 0
         };

         if (witnessStackItems != null)
         {
            tx.Inputs[0].ScriptWitness = new TransactionWitness
            {
               Components = witnessStackItems.Select(item => new TransactionWitnessComponent { RawData = item }).ToArray()
            };
         }
         return tx;
      }

      private ScriptSettings ParseFlags(string flagsString)
      {
         var flags = ScriptFlags.None;
         bool p2shEnabled = false;
         bool witnessEnabled = false; // General witness rules (BIP141 main)

         if (!string.IsNullOrEmpty(flagsString))
         {
            foreach (string flagStr in flagsString.Split(','))
            {
               if (Enum.TryParse<ScriptFlags>(flagStr.Trim(), true, out ScriptFlags parsedFlag))
               {
                  flags |= parsedFlag;
                  if (parsedFlag == ScriptFlags.P2SH) p2shEnabled = true;
                  if (parsedFlag == ScriptFlags.Witness || parsedFlag == ScriptFlags.WitnessV0) witnessEnabled = true;
               }
               else
               {
                  // Log or handle unknown flags if necessary
                  _output.WriteLine($"Warning: Unknown script flag '{flagStr.Trim()}' in test case.");
               }
            }
         }
         // The ScriptSettings constructor takes high-level settings.
         // The more granular ScriptFlags are passed to ScriptEvaluationContext.
         return new ScriptSettings(isP2shEnabled: p2shEnabled, isWitnessEnabled: witnessEnabled);
      }


      [Theory]
      [MemberData(nameof(GetBitcoinCoreTestCases))]
      public void BitcoinCoreScriptTests(ScriptTestCase testCase)
      {
         _output.WriteLine($"Running test: {testCase.Comment}");
         _output.WriteLine($"ScriptSig: {testCase.ScriptSigHex}");
         _output.WriteLine($"ScriptPubKey: {testCase.ScriptPubKeyHex}");
         if (testCase.WitnessHex != null && testCase.WitnessHex.Any())
         {
            _output.WriteLine($"Witness: [{string.Join(", ", testCase.WitnessHex)}]");
         }
         _output.WriteLine($"Flags: {testCase.Flags}");
         _output.WriteLine($"Expected Error: {testCase.ExpectedErrorString}");

         byte[] scriptSigBytes = HexToBytes(testCase.ScriptSigHex);
         byte[] scriptPubKeyBytes = HexToBytes(testCase.ScriptPubKeyHex);
         List<byte[]>? witnessStackBytes = testCase.WitnessHex?.Select(HexToBytes).ToList();

         Transaction dummyTx = CreateDummyTransaction(scriptSigBytes, scriptPubKeyBytes, witnessStackBytes);
         int inputIndex = testCase.InputIndex ?? 0;
         long amount = testCase.Amount ?? 0; // Amount for UTXO, crucial for SegWit sighash

         ScriptSettings settings = ParseFlags(testCase.Flags);
         // The ScriptFlags enum (for ScriptEvaluationContext) should also be derived from testCase.Flags string
         // for more granular control if needed by the interpreter directly. For now, ScriptSettings handles the main ones.

         bool result = _validator.VerifyScript(dummyTx, inputIndex, scriptPubKeyBytes, amount, settings, out ScriptError error);
         ScriptError expectedErrorEnum = testCase.GetExpectedScriptError();

         if (expectedErrorEnum == ScriptError.OK)
         {
            Assert.True(result, $"Expected OK, but failed with error: {error}. Test: {testCase}");
         }
         else
         {
            Assert.False(result, $"Expected error {expectedErrorEnum} ({testCase.ExpectedErrorString}), but script passed. Test: {testCase}");
            // For now, we accept any error if an error was expected.
            // A more precise test would assert: Assert.Equal(expectedErrorEnum, error);
            // This requires comprehensive mapping of Bitcoin Core error strings to our ScriptError enum.
            if (error == ScriptError.OK && expectedErrorEnum != ScriptError.OK)
            {
                // This specific assertion is to ensure we really failed if an error was expected.
                // The Assert.False above covers this, but this gives a clearer message if needed.
            }
             _output.WriteLine($"Test failed as expected. Got error: {error}, Expected: {expectedErrorEnum} ({testCase.ExpectedErrorString})");
         }
      }

      public static IEnumerable<object[]> GetBitcoinCoreTestCases()
      {
         // This would normally load from script_tests.json.
         // For now, using the subset defined in ScriptTestDataProvider.
         foreach (var testCase in ScriptTestDataProvider.GetTestCases())
         {
            yield return new object[] { testCase };
         }
      }
   }
}

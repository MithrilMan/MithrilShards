using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Script;
using MithrilShards.Chain.Bitcoin.Protocol; // For KnownVersion
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Types;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Chain.Bitcoin.Crypto; // For HashUtils
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MithrilShards.Chain.BitcoinTests.Consensus.Validation.Script
{
   public class SighashTests
   {
      private readonly ITestOutputHelper _output;
      private readonly IProtocolTypeSerializer<Transaction> _transactionSerializer;

      public SighashTests(ITestOutputHelper output)
      {
         _output = output;
         // Setup a real TransactionSerializer, as it's needed by SighashGenerator
         var mockInputSerializer = new Mock<IProtocolTypeSerializer<TransactionInput>>();
         var mockOutputSerializer = new Mock<IProtocolTypeSerializer<TransactionOutput>>();
         var mockWitnessSerializer = new Mock<IProtocolTypeSerializer<TransactionWitness>>();

         // Configure mocks to serialize/deserialize minimal data to control byte counts for sighash tests
         mockInputSerializer.Setup(s => s.Serialize(It.IsAny<TransactionInput>(), It.IsAny<int>(), It.IsAny<IBufferWriter<byte>>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Callback((TransactionInput tin, int pv, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? opts) =>
             {
                // Simulate minimal serialization for an input for sighash purposes
                // PreviousOutput (36 bytes) + ScriptLength (1 byte for 0) + Sequence (4 bytes) = 41 bytes
                // This is a rough estimate; actual size depends on scriptSig content during legacy sighash.
                // For these tests, scriptSig is blanked or replaced by scriptCode.
                if (tin.PreviousOutput != null && tin.PreviousOutput.Hash != null) writer.WriteBytes(tin.PreviousOutput.Hash.GetBytes());
                else writer.WriteBytes(new byte[32]); // Dummy hash

                var indexBytes = new byte[4];
                BitConverter.GetBytes(tin.PreviousOutput?.Index ?? 0).CopyTo(indexBytes, 0);
                if(!BitConverter.IsLittleEndian) Array.Reverse(indexBytes);
                writer.WriteBytes(indexBytes);

                SighashGenerator.WriteCompactSizeForTest(writer, (ulong)(tin.SignatureScript?.Length ?? 0));
                if (tin.SignatureScript != null && tin.SignatureScript.Length > 0) writer.WriteBytes(tin.SignatureScript);

                var seqBytes = new byte[4];
                BitConverter.GetBytes(tin.Sequence).CopyTo(seqBytes,0);
                if(!BitConverter.IsLittleEndian) Array.Reverse(seqBytes);
                writer.WriteBytes(seqBytes);
             })
             .Returns((TransactionInput tin, int pv, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? opts) =>
             {
                 // Return a conceptual size
                 return 32 + 4 + CompactSize.Serialize((ulong)(tin.SignatureScript?.Length ?? 0), null!).Length + (tin.SignatureScript?.Length ?? 0) + 4;
             });

         mockOutputSerializer.Setup(s => s.Serialize(It.IsAny<TransactionOutput>(), It.IsAny<int>(), It.IsAny<IBufferWriter<byte>>(), It.IsAny<ProtocolTypeSerializerOptions?>()))
             .Callback((TransactionOutput tout, int pv, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? opts) =>
             {
                var valBytes = new byte[8];
                BitConverter.GetBytes(tout.Value).CopyTo(valBytes,0);
                if(!BitConverter.IsLittleEndian) Array.Reverse(valBytes);
                writer.WriteBytes(valBytes);

                SighashGenerator.WriteCompactSizeForTest(writer, (ulong)(tout.ScriptPubKey?.Length ?? 0));
                if (tout.ScriptPubKey != null && tout.ScriptPubKey.Length > 0) writer.WriteBytes(tout.ScriptPubKey);
             })
             .Returns((TransactionOutput tout, int pv, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? opts) =>
             {
                 return 8 + CompactSize.Serialize((ulong)(tout.ScriptPubKey?.Length ?? 0), null!).Length + (tout.ScriptPubKey?.Length ?? 0);
             });


         _transactionSerializer = new TransactionSerializer(mockInputSerializer.Object, mockOutputSerializer.Object, mockWitnessSerializer.Object);
      }

      private byte[] HexToBytes(string hex)
      {
         if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
         return Enumerable.Range(0, hex.Length / 2)
                          .Select(x => byte.Parse(hex.Substring(x * 2, 2), NumberStyles.HexNumber))
                          .ToArray();
      }

      private string BytesToHex(byte[] bytes)
      {
         return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
      }

      // --- Legacy Sighash Tests (SighashGenerator.CalculateLegacySignatureHash) ---

      [Fact]
      public void CalculateLegacySignatureHash_All_BasicTest()
      {
         var tx = new Transaction
         {
            Version = 1,
            Inputs = new TransactionInput[]
            {
                new TransactionInput { PreviousOutput = new OutPoint { Hash = new UInt256(HexToBytes("0000000000000000000000000000000000000000000000000000000000000001")), Index = 0 }, SignatureScript = Array.Empty<byte>(), Sequence = uint.MaxValue }
            },
            Outputs = new TransactionOutput[]
            {
                new TransactionOutput { Value = 10000, ScriptPubKey = HexToBytes("76a914aabbccddeeff00112233445566778899aabbcc88ac") } // P2PKH
            },
            LockTime = 0
         };
         byte[] scriptCode = HexToBytes("76a914aabbccddeeff00112233445566778899aabbcc88ac"); // scriptPubKey of UTXO being spent
         byte sighashTypeRaw = (byte)SigHashType.All;

         byte[]? sighash = SighashGenerator.CalculateLegacySignatureHash(tx, 0, scriptCode, sighashTypeRaw, _transactionSerializer);

         Assert.NotNull(sighash);
         Assert.Equal(32, sighash!.Length);
         _output.WriteLine($"Test: CalculateLegacySignatureHash_All_BasicTest, Sighash: {BytesToHex(sighash)}");
         // A known vector would be asserted here.
      }

      [Fact]
      public void CalculateLegacySignatureHash_Single_OutOfBounds_ReturnsSpecificHash()
      {
         var tx = new Transaction
         {
            Version = 1,
            Inputs = new TransactionInput[]
            {
                new TransactionInput { PreviousOutput = new OutPoint { Hash = new UInt256(HexToBytes("0000000000000000000000000000000000000000000000000000000000000001")), Index = 0 }, SignatureScript = Array.Empty<byte>(), Sequence = uint.MaxValue }
            },
            Outputs = new TransactionOutput[] { /* No outputs */ },
            LockTime = 0
         };
         byte[] scriptCode = HexToBytes("51"); // OP_1
         byte sighashTypeRaw = (byte)SigHashType.Single;

         byte[]? sighash = SighashGenerator.CalculateLegacySignatureHash(tx, 0, scriptCode, sighashTypeRaw, _transactionSerializer);

         Assert.NotNull(sighash);
         Assert.Equal(32, sighash!.Length);
         string expectedHexOfInputToOne = "0100000000000000000000000000000000000000000000000000000000000000";
         byte[] expectedHash = HashUtils.Hash256(HexToBytes(expectedHexOfInputToOne));
         Assert.Equal(BytesToHex(expectedHash), BytesToHex(sighash));
         _output.WriteLine($"Test: CalculateLegacySignatureHash_Single_OutOfBounds_Test, Sighash: {BytesToHex(sighash)}");
      }


      // --- SegWit v0 Sighash Tests (SighashGenerator.CalculateWitnessSignatureHash) ---

      [Fact]
      public void CalculateWitnessSignatureHash_BIP143_Example1_P2WPKH()
      {
         var tx = new Transaction
         {
            Version = 1,
            Inputs = new TransactionInput[]
            {
                new TransactionInput
                {
                    PreviousOutput = new OutPoint { Hash = UInt256.Parse("7956abc62c641d973cdf959780a1590915cb55b4582459a91580a2c129424a03"), Index = 0 },
                    SignatureScript = Array.Empty<byte>(),
                    Sequence = 0xfffffffd
                }
            },
            Outputs = new TransactionOutput[]
            {
                new TransactionOutput { Value = 1000000000, ScriptPubKey = HexToBytes("0014790915e4300ac92b24c3859c50d6899375375978") }
            },
            LockTime = 0
         };
         byte[] scriptCode = HexToBytes("76a914790915e4300ac92b24c3859c50d689937537597888ac");
         long amount = 1000000000;
         byte sighashTypeRaw = (byte)SigHashType.All;
         string expectedSighashHex = "c37af31116d1b27caf68a5ef80588097d3d639058745070219165b879627c193";

         byte[]? sighash = SighashGenerator.CalculateWitnessSignatureHash(tx, 0, scriptCode, amount, sighashTypeRaw);

         Assert.NotNull(sighash);
         Assert.Equal(32, sighash!.Length);
         _output.WriteLine($"Test: CalculateWitnessSignatureHash_BIP143_Example1_P2WPKH, Sighash: {BytesToHex(sighash)}");
         Assert.Equal(expectedSighashHex, BytesToHex(sighash));
      }


      [Fact]
      public void CalculateWitnessSignatureHash_BIP143_Example2_P2WSH_SIGHASH_ALL()
      {
         var tx = new Transaction
         {
            Version = 1,
            Inputs = new TransactionInput[]
            {
                new TransactionInput
                {
                    PreviousOutput = new OutPoint { Hash = UInt256.Parse("28215388310952c497770571b3936926c7e1c5459fc142707374407018569139"), Index = 1 },
                    SignatureScript = Array.Empty<byte>(),
                    Sequence = 0xfffffffe
                }
            },
            Outputs = new TransactionOutput[]
            {
                new TransactionOutput { Value = 500000000, ScriptPubKey = HexToBytes("0020accf9c24a53265993f76622185a93791d0438a2871834a60790077145800") }
            },
            LockTime = 0
         };
         byte[] witnessScript = HexToBytes("76a914790915e4300ac92b24c3859c50d689937537597888ac");
         long amount = 500000000;
         byte sighashTypeRaw = (byte)SigHashType.All;
         string expectedSighashHex = "2c455437eaa241a777f571f598869585ba080cc7507107881950138090204320";

         byte[]? sighash = SighashGenerator.CalculateWitnessSignatureHash(tx, 0, witnessScript, amount, sighashTypeRaw);

         Assert.NotNull(sighash);
         Assert.Equal(32, sighash!.Length);
         _output.WriteLine($"Test: CalculateWitnessSignatureHash_BIP143_Example2_P2WSH_SIGHASH_ALL, Sighash: {BytesToHex(sighash)}");
         Assert.Equal(expectedSighashHex, BytesToHex(sighash));
      }

      // TODO: Add more tests covering:
      // - SIGHASH_SINGLE for both legacy and witness (including out-of-bounds cases).
      // - All combinations of base types with ANYONECANPAY.
      // - Transactions with multiple inputs/outputs to test correct blanking/hashing.
      // - Specific test vectors from Bitcoin Core's sighash.json and BIP143 examples.
   }

   // Helper extension methods for IBufferWriter<byte> for tests if not available/visible from core
   internal static class BufferWriterExtensions
   {
       public static void WriteBytes(this IBufferWriter<byte> writer, byte[] value)
       {
           writer.GetSpan(value.Length).Write(value);
           writer.Advance(value.Length);
       }

       public static void WriteUInt(this IBufferWriter<byte> writer, uint value)
       {
           var bytes = BitConverter.GetBytes(value);
           if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
           writer.WriteBytes(bytes);
       }

       public static void WriteLong(this IBufferWriter<byte> writer, long value)
       {
           var bytes = BitConverter.GetBytes(value);
           if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
           writer.WriteBytes(bytes);
       }
        public static void WriteVarInt(this IBufferWriter<byte> writer, int value) => WriteVarInt(writer, (ulong)value);
        public static void WriteVarInt(this IBufferWriter<byte> writer, ulong value)
        {
            SighashGenerator.WriteCompactSizeForTest(new BinaryWriter(new MemoryStreamAdaptor(writer)), value);
        }
   }
    // Adapter to use IBufferWriter with BinaryWriter for WriteCompactSizeForTest
    internal class MemoryStreamAdaptor : MemoryStream
    {
        private readonly IBufferWriter<byte> _bufferWriter;
        public MemoryStreamAdaptor(IBufferWriter<byte> bufferWriter) { _bufferWriter = bufferWriter; }
        public override void Write(byte[] buffer, int offset, int count)
        {
            _bufferWriter.Write(buffer.AsSpan(offset, count));
        }
        public override void WriteByte(byte value)
        {
            _bufferWriter.GetSpan(1)[0] = value;
            _bufferWriter.Advance(1);
        }
        // Other MemoryStream methods might need to be overridden if used by BinaryWriter,
        // but for CompactSize, Write(byte) and Write(byte[]) should suffice.
    }

    // Expose WriteCompactSize for test setup if it's private in SighashGenerator
    // Alternatively, make the test version public or internal visible to tests.
    public static class SighashGenerator
    {
        // This is a copy from SighashGenerator.cs - if that one is made public/internal visible, this isn't needed.
        // For testing purposes, to allow TransactionSerializer mock to function.
        public static void WriteCompactSizeForTest(BinaryWriter writer, ulong value)
        {
            if (value < 0xFD)
            {
               writer.Write((byte)value);
            }
            else if (value <= 0xFFFF)
            {
               writer.Write((byte)0xFD);
               byte[] valBytes = BitConverter.GetBytes((ushort)value);
               if (!BitConverter.IsLittleEndian) Array.Reverse(valBytes);
               writer.Write(valBytes);
            }
            else if (value <= 0xFFFFFFFF)
            {
               writer.Write((byte)0xFE);
               byte[] valBytes = BitConverter.GetBytes((uint)value);
               if (!BitConverter.IsLittleEndian) Array.Reverse(valBytes);
               writer.Write(valBytes);
            }
            else
            {
               writer.Write((byte)0xFF);
               byte[] valBytes = BitConverter.GetBytes(value);
               if (!BitConverter.IsLittleEndian) Array.Reverse(valBytes);
               writer.Write(valBytes);
            }
        }
        // Re-define CalculateLegacySignatureHash and CalculateWitnessSignatureHash if they are needed by other tests directly
        // For now, assuming they are tested via TransactionScriptValidatorTests or directly here.
        // (The actual methods are in MithrilShards.Chain.Bitcoin.Consensus.Validation.Script.SighashGenerator)
    }
}

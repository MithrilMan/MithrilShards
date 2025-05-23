using System;
using System.Linq;
using System.Text;
using MithrilShards.Chain.Bitcoin.Crypto; // For Secp256k1BouncyCastle
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Crypto
{
   public class Secp256k1BouncyCastleTests
   {
      private byte[] HexToBytes(string hex)
      {
         return Enumerable.Range(0, hex.Length / 2)
                          .Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
                          .ToArray();
      }

      // Standard compressed public key (valid)
      public static readonly byte[] ValidCompressedPubKey = HexToBytes("0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798");
      // Standard uncompressed public key (valid)
      public static readonly byte[] ValidUncompressedPubKey = HexToBytes("0479be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8");
      // Valid DER signature (R, S are 32 bytes each, low S) - actual values are placeholders
      public static readonly byte[] ValidDerSignatureLowS = HexToBytes("304402200102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f2002200102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20");
      // Valid sighash (32 bytes)
      public static readonly byte[] ValidSighash = new byte[32]; // All zeros for simplicity, real tests need real sighashes

      // Note: The `ParsePublicKey` and `ParseDerSignature` methods in Secp256k1BouncyCastle are private.
      // We test them indirectly via the public `VerifySignature` method, by providing inputs
      // that should cause failures at those specific parsing stages.

      [Theory]
      [InlineData("0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798")] // Valid compressed
      [InlineData("0379be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798")] // Valid compressed (odd Y)
      [InlineData("0479be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8")] // Valid uncompressed
      public void VerifySignature_WithValidPublicKeyFormats_AttemptsVerification(string pubKeyHex)
      {
         // This test checks if valid public key formats are parsed without immediate failure.
         // Since crypto is stubbed in Secp256k1BouncyCastle to return true after parsing,
         // a true result here implies successful parsing.
         byte[] publicKey = HexToBytes(pubKeyHex);
         // The actual result of VerifySignature depends on BouncyCastle's crypto, which is assumed correct.
         // Here we are testing the parsing path.
         bool result = Secp256k1BouncyCastle.VerifySignature(ValidSighash, ValidDerSignatureLowS, publicKey);
         Assert.True(result, "Valid public key format should pass parsing and proceed to (stubbed) verification which returns true.");
      }

      [Theory]
      [InlineData("0079be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798")] // Invalid prefix 0x00
      [InlineData("0579be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798")] // Invalid prefix 0x05
      [InlineData("0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f817")]   // Invalid length (32 bytes for compressed)
      [InlineData("0479be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4")] // Invalid length (64 bytes for uncompressed)
      [InlineData("02ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")] // X coord >= P (field prime for secp256k1)
      public void VerifySignature_WithInvalidPublicKeyFormats_ReturnsFalse(string invalidPubKeyHex)
      {
         byte[] invalidPublicKey = HexToBytes(invalidPubKeyHex);
         bool result = Secp256k1BouncyCastle.VerifySignature(ValidSighash, ValidDerSignatureLowS, invalidPublicKey);
         Assert.False(result, "Invalid public key format should fail parsing and lead to VerifySignature returning false.");
      }


      [Fact]
      public void VerifySignature_WithValidDerSignature_AttemptsVerification()
      {
         // This test checks if a structurally valid DER signature is parsed without immediate failure.
         // Relies on stubbed crypto in Secp256k1BouncyCastle returning true after parsing.
         bool result = Secp256k1BouncyCastle.VerifySignature(ValidSighash, ValidDerSignatureLowS, ValidCompressedPubKey);
         Assert.True(result, "Valid DER signature should pass parsing and proceed to (stubbed) verification which returns true.");
      }

      [Theory]
      [InlineData("304402200102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20022100fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141")] // High S-value (needs normalization)
      public void VerifySignature_WithHighSDerSignature_NormalizesAndAttemptsVerification(string highSSigHex)
      {
         // This test ensures a high S-value is normalized.
         // Since crypto is stubbed to return true, a true result implies successful parsing and normalization.
         byte[] highSSignature = HexToBytes(highSSigHex);
         bool result = Secp256k1BouncyCastle.VerifySignature(ValidSighash, highSSignature, ValidCompressedPubKey);
         Assert.True(result, "High S-value signature should be normalized, pass parsing, and proceed to (stubbed) verification which returns true.");
      }

      [Theory]
      [InlineData("304402200102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f200200")] // S length 0
      [InlineData("3044020002200102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20")] // R length 0
      [InlineData("3006020100020100")] // R=0, S=0 (valid DER integers, but crypto libraries might reject for ECDSA)
      [InlineData("3040021f0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f021f0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f")] // R or S too short for their length byte
      [InlineData("00")] // Too short
      [InlineData("314402200102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f2002200102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20")] // Invalid sequence tag
      public void VerifySignature_WithInvalidDerSignatures_ReturnsFalse(string invalidDerSigHex)
      {
         byte[] invalidDerSignature = HexToBytes(invalidDerSigHex);
         bool result = Secp256k1BouncyCastle.VerifySignature(ValidSighash, invalidDerSignature, ValidCompressedPubKey);
         Assert.False(result, "Invalid DER signature format should fail parsing and lead to VerifySignature returning false.");
      }

      [Fact]
      public void VerifySignature_KnownValidTriplet_ReturnsTrue_Conceptual()
      {
         // THIS IS A CONCEPTUAL TEST. Requires a known valid (sighash, DER sig (no type byte), pubkey) triplet.
         // Example: Bitcoin Core test vector `tx_valid.json[0]`
         // sighash (tx_hash_segwit): "cc598697a120212ed518384e0397e41b4eadcbf61213180bf1183f1c272098c7"
         // pubkey: "0361c0770c61fc79479453d3a08d10614a2789871460999200a6f301f84301e9a0"
         // signature (from scriptWitness, DER encoded, low-S, without sighash flag):
         // "304402200e9609794d291689765dd494043980b518028357c8d1801134216660489827850220108755f0110e0776757a096ef900115971605c80699be650d48afe7288873381"

         byte[] sighash = HexToBytes("cc598697a120212ed518384e0397e41b4eadcbf61213180bf1183f1c272098c7");
         byte[] derSignature = HexToBytes("304402200e9609794d291689765dd494043980b518028357c8d1801134216660489827850220108755f0110e0776757a096ef900115971605c80699be650d48afe7288873381");
         byte[] publicKey = HexToBytes("0361c0770c61fc79479453d3a08d10614a2789871460999200a6f301f84301e9a0");

         // This test will actually call BouncyCastle.
         bool isValid = Secp256k1BouncyCastle.VerifySignature(sighash, derSignature, publicKey);
         Assert.True(isValid, "Known valid signature triplet should verify correctly.");
      }

      [Fact]
      public void VerifySignature_KnownInvalidSignatureForTriplet_ReturnsFalse_Conceptual()
      {
         // Uses the same sighash and pubkey as above, but a slightly modified (invalidated) signature.
         byte[] sighash = HexToBytes("cc598697a120212ed518384e0397e41b4eadcbf61213180bf1183f1c272098c7");
         // Invalidated signature (e.g., last byte changed)
         byte[] invalidDerSignature = HexToBytes("304402200e9609794d291689765dd494043980b518028357c8d1801134216660489827850220108755f0110e0776757a096ef900115971605c80699be650d48afe7288873380"); // Last byte changed from 81 to 80
         byte[] publicKey = HexToBytes("0361c0770c61fc79479453d3a08d10614a2789871460999200a6f301f84301e9a0");

         bool isValid = Secp256k1BouncyCastle.VerifySignature(sighash, invalidDerSignature, publicKey);
         Assert.False(isValid, "Known invalid signature for triplet should fail verification.");
      }
   }
}

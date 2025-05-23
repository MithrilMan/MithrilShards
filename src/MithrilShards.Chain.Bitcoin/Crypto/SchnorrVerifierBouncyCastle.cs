using System;
using System.Diagnostics;
using Org.BouncyCastle.Asn1.Sec; // For SecNamedCurves
using Org.BouncyCastle.Crypto.Parameters; // For ECPublicKeyParameters, ECDomainParameters
using Org.BouncyCastle.Math; // For BigInteger
using Org.BouncyCastle.Math.EC; // For ECPoint

namespace MithrilShards.Chain.Bitcoin.Crypto
{
   /// <summary>
   /// Provides BIP340-compliant Schnorr signature verification using the BouncyCastle cryptography library.
   ///
   /// --- IMPORTANT SIMULATION NOTE ---
   /// This class SIMULATES the interaction with BouncyCastle for BIP340 Schnorr signatures.
   /// The actual cryptographic operations for Schnorr verification and the precise parsing of
   /// x-only public keys and 64-byte (R,S) signatures are NOT fully implemented here due to
   /// environment limitations for verifying BouncyCastle's exact BIP340 API.
   /// This class serves as an integration point.
   /// Unit tests relying on this will only pass if a real library correctly implements BIP340.
   /// --- END IMPORTANT SIMULATION NOTE ---
   /// </summary>
   public static class SchnorrVerifierBouncyCastle
   {
      private static readonly X9ECParameters _secp256k1CurveParams;
      private static readonly ECDomainParameters _secp256k1DomainParams;
      private static readonly BigInteger _secp256k1OrderN; // Order N of the curve (for R and S checks)
      private static readonly BigInteger _secp256k1FieldP; // Field characteristic P (for X coordinate check)


      static SchnorrVerifierBouncyCastle()
      {
         _secp256k1CurveParams = SecNamedCurves.GetByName("secp256k1");
         if (_secp256k1CurveParams == null)
         {
            throw new InvalidOperationException("secp256k1 curve not found in BouncyCastle.");
         }
         _secp256k1DomainParams = new ECDomainParameters(
             _secp256k1CurveParams.Curve,
             _secp256k1CurveParams.G,
             _secp256k1CurveParams.N,
             _secp256k1CurveParams.H,
             _secp256k1CurveParams.GetSeed());

         _secp256k1OrderN = _secp256k1CurveParams.N;
         _secp256k1FieldP = _secp256k1CurveParams.Curve.Field.Characteristic;
      }

      /// <summary>
      /// Parses a 32-byte x-only public key into BouncyCastle ECPublicKeyParameters.
      /// BIP340 specifies implicitly choosing the even Y coordinate.
      /// </summary>
      private static ECPublicKeyParameters? ParseXOnlyPublicKey(byte[] xOnlyPublicKeyBytes)
      {
         if (xOnlyPublicKeyBytes == null || xOnlyPublicKeyBytes.Length != 32)
         {
            Debug.WriteLine("SchnorrVerifierBouncyCastle.ParseXOnlyPublicKey: Public key must be 32 bytes.");
            return null;
         }

         try
         {
            BigInteger x = new BigInteger(1, xOnlyPublicKeyBytes);
            if (x.CompareTo(_secp256k1FieldP) >= 0) // x must be < p
            {
                Debug.WriteLine("SchnorrVerifierBouncyCastle.ParseXOnlyPublicKey: Public key x-coordinate is too large.");
                return null;
            }

            // Reconstruct the ECPoint from x-coordinate, choosing even y.
            // y^2 = x^3 + ax + b (secp256k1: y^2 = x^3 + 7)
            // This requires functions to calculate square root modulo p and check for even y.
            // BouncyCastle's ECPoint.Decompress() or similar might handle this if an appropriate prefix (0x02) is added.
            // Or, Curve.DecodePoint with a prepended 0x02 byte for even Y.
            byte[] compressedPubKey = new byte[33];
            compressedPubKey[0] = 0x02; // Indicates even Y
            Buffer.BlockCopy(xOnlyPublicKeyBytes, 0, compressedPubKey, 1, 32);

            ECPoint q = _secp256k1CurveParams.Curve.DecodePoint(compressedPubKey);
            // DecodePoint also validates if the point is on the curve.
            return new ECPublicKeyParameters(q, _secp256k1DomainParams);
         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Error parsing x-only public key with BouncyCastle: {ex.Message}");
            return null;
         }
      }

      /// <summary>
      /// Verifies a BIP340 Schnorr signature.
      /// </summary>
      /// <param name="sighash">The 32-byte message hash (already tagged as per BIP340 if necessary).</param>
      /// <param name="signature">The 64-byte Schnorr signature (R, S).</param>
      /// <param name="xOnlyPublicKey">The 32-byte x-only public key.</param>
      /// <returns>True if the signature is valid, false otherwise.</returns>
      public static bool VerifyBip340Signature(byte[] sighash, byte[] signature, byte[] xOnlyPublicKey)
      {
         if (sighash == null || sighash.Length != 32)
         {
            Debug.WriteLine("SchnorrVerifierBouncyCastle.VerifyBip340Signature: Invalid sighash provided (must be 32 bytes).");
            return false;
         }
         if (signature == null || signature.Length != 64)
         {
            Debug.WriteLine("SchnorrVerifierBouncyCastle.VerifyBip340Signature: Invalid signature provided (must be 64 bytes).");
            return false;
         }
         // xOnlyPublicKey null/length check is handled by ParseXOnlyPublicKey

         ECPublicKeyParameters? pubKeyParams = ParseXOnlyPublicKey(xOnlyPublicKey);
         if (pubKeyParams == null)
         {
            Debug.WriteLine("SchnorrVerifierBouncyCastle.VerifyBip340Signature: Public key parsing failed.");
            return false;
         }

         // Extract R and S from the 64-byte signature
         byte[] rBytes = new byte[32];
         byte[] sBytes = new byte[32];
         Buffer.BlockCopy(signature, 0, rBytes, 0, 32);
         Buffer.BlockCopy(signature, 32, sBytes, 0, 32);

         BigInteger r = new BigInteger(1, rBytes);
         BigInteger s = new BigInteger(1, sBytes);

         // Validate R and S components (must be less than curve order N)
         if (r.CompareTo(_secp256k1OrderN) >= 0 || s.CompareTo(_secp256k1OrderN) >= 0)
         {
            Debug.WriteLine("SchnorrVerifierBouncyCastle.VerifyBip340Signature: Signature R or S value is too large.");
            return false;
         }

         // --- SIMULATED BouncyCastle SchnorrSigner Interaction ---
         // BouncyCastle might have a specific SchnorrSigner or a generic one adaptable to BIP340.
         // The core BIP340 verification equation: sG = R + hash(R, P, m)P
         // This involves:
         // 1. Computing `e = int(hash("BIP0340/challenge" || R.x || P.x || m)) mod n`.
         //    (Note: sighash provided to this function is assumed to be `m` already, potentially tagged)
         //    The actual challenge hash `e` for BIP340 uses a specific tagged hash:
         //    `e = int(tagged_hash("BIP0340/challenge", bytes(r) + bytes(P) + m)) mod n`
         //    where `bytes(P)` is the 32-byte x-coordinate of P.
         //    This is slightly different from how `m` (sighash) is passed here.
         //    A library might abstract this, or expect `e` directly, or expect `m` to be the final message.
         //    For now, we assume `sighash` is the `m` to be used in the challenge hash construction.
         //
         // 2. Verifying R is the x-coordinate of `sG - eP`.
         //
         // Example using a hypothetical BouncyCastle SchnorrSigner:
         // ISigner schnorrSigner = new SomeBouncyCastleSchnorrSigner(new SomeChallengeHashFunction()); // Or similar
         // schnorrSigner.Init(false, pubKeyParams);
         // schnorrSigner.BlockUpdate(sighash, 0, sighash.Length); // Or the components for challenge hash
         // bool isValid = schnorrSigner.VerifySignature(Concatenated_R_and_S_or_just_S_depending_on_API);

         // --- Start of BIP340 Verification Logic ---

         // 1. Let P = lift_x(int(pk))
         //    lift_x(x) is the point P for which x(P) = x and has_even_y(P) is true.
         //    This is handled by ParseXOnlyPublicKey. pubKeyParams.Q is our P.
         ECPoint P = pubKeyParams.Q;

         // 2. Let r = int(sig[0:32]); fail if r >= p (field size).
         //    Note: BIP340 actually requires r < n (curve order), but r < p is also implicitly true for valid points.
         //    The check r < n was already done when parsing r_bigInt.
         //    The x-coordinate of R (the point) is r.

         // 3. Let s = int(sig[32:64]); fail if s >= n (curve order).
         //    This was already done when parsing s_bigInt.

         // 4. Let e = int(hashBIP0340/challenge(bytes(r) || bytes(x(P)) || m)) mod n.
         //    bytes(x(P)) is xOnlyPublicKey.
         //    m is the sighash.
         byte[] challengeInput = new byte[32 + 32 + 32]; // r_bytes(32) || P_x_bytes(32) || message_hash(32)
         Buffer.BlockCopy(rBytes, 0, challengeInput, 0, 32);
         Buffer.BlockCopy(xOnlyPublicKey, 0, challengeInput, 32, 32);
         Buffer.BlockCopy(sighash, 0, challengeInput, 64, 32);

         byte[] e_hash = HashUtils.TaggedHash("BIP0340/challenge", challengeInput);
         BigInteger e_num = new BigInteger(1, e_hash);
         e_num = e_num.Mod(_secp256k1OrderN); // Reduce modulo N

         // 5. Let R = sG - eP.
         //    sG = G.Multiply(s)
         //    eP = P.Multiply(e)
         //    R = sG + (-eP) = sG + P.Multiply(e.Negate().Mod(N))
         //    (Using Add and Negate is common in EC libraries)

         ECPoint sG = _secp256k1DomainParams.G.Multiply(s);
         BigInteger e_neg = e_num.Equals(BigInteger.Zero) ? BigInteger.Zero : _secp256k1OrderN.Subtract(e_num); // -e mod N
         ECPoint neg_eP = P.Multiply(e_neg); // P.Multiply(e).Negate() might also work if implemented robustly for ECPoint
                                             // but P.Multiply(-e mod N) is generally safer.

         ECPoint R_check_point = sG.Add(neg_eP).Normalize(); // Normalize to ensure coordinates are available and consistent

         // 6. Fail if R is infinity.
         if (R_check_point.IsInfinity)
         {
            Debug.WriteLine("SchnorrVerifierBouncyCastle.VerifyBip340Signature: R_check_point is infinity.");
            return false;
         }

         // 7. Fail if R has an odd y-coordinate. (has_even_y(R) is false)
         //    In BouncyCastle, ECFieldElement.TestBit(0) checks the LSB. False if LSB=0 (even), True if LSB=1 (odd).
         if (R_check_point.YCoord.ToBigInteger().TestBit(0)) // If Y is odd
         {
            Debug.WriteLine("SchnorrVerifierBouncyCastle.VerifyBip340Signature: R_check_point has an odd Y coordinate.");
            return false;
         }

         // 8. Fail if x(R) is not equal to r.
         if (!R_check_point.XCoord.ToBigInteger().Equals(r))
         {
            Debug.WriteLine("SchnorrVerifierBouncyCastle.VerifyBip340Signature: R_check_point's X coordinate does not match r from signature.");
            return false;
         }

         // All checks passed.
         return true;
      }
   }
}

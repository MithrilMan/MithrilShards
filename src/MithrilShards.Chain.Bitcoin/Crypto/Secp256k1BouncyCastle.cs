using System;
using System.Diagnostics;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace MithrilShards.Chain.Bitcoin.Crypto
{
   /// <summary>
   /// Provides ECDSA secp256k1 signature verification using the BouncyCastle cryptography library.
   /// </summary>
   public static class Secp256k1BouncyCastle
   {
      private static readonly X9ECParameters _secp256k1CurveParams;
      private static readonly ECDomainParameters _secp256k1DomainParams;
      private static readonly BigInteger _secp256k1OrderN; // Order N of the curve
      private static readonly BigInteger _halfOrderN; // N/2, used for low-S normalization

      static Secp256k1BouncyCastle()
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
         _halfOrderN = _secp256k1OrderN.ShiftRight(1); // N/2
      }

      /// <summary>
      /// Parses raw public key bytes into BouncyCastle ECPublicKeyParameters.
      /// </summary>
      private static ECPublicKeyParameters? ParsePublicKey(byte[] publicKeyBytes)
      {
         if (publicKeyBytes == null || publicKeyBytes.Length < 1) return null;
         try
         {
            ECPoint q = _secp256k1CurveParams.Curve.DecodePoint(publicKeyBytes);
            // Basic validation of the point (e.g., ensure it's on the curve) is done by DecodePoint and later by ECDsaSigner.
            return new ECPublicKeyParameters(q, _secp256k1DomainParams);
         }
         catch (Exception ex) // Catches format errors from DecodePoint
         {
            Debug.WriteLine($"Error parsing public key with BouncyCastle: {ex.Message}");
            return null;
         }
      }

      /// <summary>
      /// Parses a DER-encoded signature into its R and S components.
      /// Also normalizes S to be a low-S value as per BIP62.
      /// </summary>
      private static bool ParseDerSignature(byte[] derSignature, out BigInteger r, out BigInteger s)
      {
         r = BigInteger.Zero;
         s = BigInteger.Zero;

         if (derSignature == null || derSignature.Length == 0) return false;

         try
         {
            using (var asn1Stream = new Asn1InputStream(derSignature))
            {
               var seq = asn1Stream.ReadObject() as DerSequence;
               if (seq == null || seq.Count != 2) return false;

               var rDer = seq[0] as DerInteger;
               var sDer = seq[1] as DerInteger;

               if (rDer == null || sDer == null) return false;

               r = rDer.Value;
               s = sDer.Value;

               // Low-S Normalization (BIP62)
               // If s > N/2, then s = N - s.
               if (s.CompareTo(_halfOrderN) > 0)
               {
                  s = _secp256k1OrderN.Subtract(s);
               }
               return true;
            }
         }
         catch (Exception ex) // Catches ASN.1 parsing errors
         {
            Debug.WriteLine($"Error parsing DER signature with BouncyCastle: {ex.Message}");
            return false;
         }
      }


      /// <summary>
      /// Verifies an ECDSA signature against a public key and a message hash using the secp256k1 curve
      /// and BouncyCastle for cryptographic operations.
      /// </summary>
      /// <param name="sighash">The 32-byte message hash that was signed.</param>
      /// <param name="derSignature">The raw DER-encoded signature bytes (excluding the sighash type byte).</param>
      /// <param name="publicKeyBytes">The raw public key bytes (compressed or uncompressed format).</param>
      /// <returns>True if the signature is valid, false otherwise.</returns>
      public static bool VerifySignature(byte[] sighash, byte[] derSignature, byte[] publicKeyBytes)
      {
         if (sighash == null || sighash.Length != 32)
         {
            Debug.WriteLine("Secp256k1BouncyCastle.VerifySignature: Invalid sighash provided.");
            return false;
         }
         if (derSignature == null || derSignature.Length == 0)
         {
            Debug.WriteLine("Secp256k1BouncyCastle.VerifySignature: Empty DER signature provided.");
            return false;
         }
         if (publicKeyBytes == null || publicKeyBytes.Length == 0)
         {
            Debug.WriteLine("Secp256k1BouncyCastle.VerifySignature: Empty public key provided.");
            return false;
         }

         ECPublicKeyParameters? pubKeyParams = ParsePublicKey(publicKeyBytes);
         if (pubKeyParams == null)
         {
            Debug.WriteLine("Secp256k1BouncyCastle.VerifySignature: Public key parsing failed.");
            return false;
         }

         if (!ParseDerSignature(derSignature, out BigInteger r, out BigInteger s))
         {
            Debug.WriteLine("Secp256k1BouncyCastle.VerifySignature: DER signature parsing or S-value normalization failed.");
            return false;
         }

         try
         {
            ECDsaSigner signer = new ECDsaSigner();
            signer.Init(false, pubKeyParams); // Init for verification
            return signer.VerifySignature(sighash, r, s);
         }
         catch (Exception ex)
         {
            // Catch any unexpected errors during BouncyCastle's verification.
            Debug.WriteLine($"Error during BouncyCastle ECDSA verification: {ex.Message}");
            return false;
         }
      }
   }
}

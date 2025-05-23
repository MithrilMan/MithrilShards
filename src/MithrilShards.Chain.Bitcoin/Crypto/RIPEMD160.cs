using System;
using System.Diagnostics;
// Required for BouncyCastle RIPEMD160
using Org.BouncyCastle.Crypto.Digests;

namespace MithrilShards.Chain.Bitcoin.Crypto
{
   /// <summary>
   /// Provides an implementation of the RIPEMD-160 hash algorithm
   /// using the BouncyCastle cryptography library.
   /// </summary>
   public static class RIPEMD160
   {
      public const int HASH_BYTE_SIZE = 20; // 160 bits

      /// <summary>
      /// Computes the RIPEMD-160 hash of the specified byte array using BouncyCastle.
      /// </summary>
      /// <param name="data">The input data to hash.</param>
      /// <returns>A 20-byte array representing the RIPEMD-160 hash.</returns>
      /// <exception cref="ArgumentNullException">Thrown if the input data is null.</exception>
      public static byte[] ComputeHash(byte[] data)
      {
         if (data == null)
         {
            throw new ArgumentNullException(nameof(data));
         }

         try
         {
            RipeMD160Digest digest = new RipeMD160Digest();
            digest.BlockUpdate(data, 0, data.Length);
            byte[] hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);

            if (hash.Length != HASH_BYTE_SIZE)
            {
               // This should not happen with BouncyCastle's RipeMD160Digest.
               Debug.WriteLine($"Warning: BouncyCastle RIPEMD160 implementation returned a hash of unexpected length: {hash.Length} bytes.");
               throw new InvalidOperationException($"RIPEMD-160 implementation (BouncyCastle) returned a hash of {hash.Length} bytes, expected {HASH_BYTE_SIZE} bytes.");
            }
            return hash;
         }
         catch (Exception ex) // Catch any potential exceptions from the crypto library.
         {
            Debug.WriteLine($"ERROR: An unexpected error occurred during RIPEMD160 computation using BouncyCastle: {ex.Message}");
            // Consider specific exception types from BouncyCastle if known, or rethrow wrapped.
            throw new InvalidOperationException("An unexpected error occurred during RIPEMD-160 computation using BouncyCastle. See inner exception for details.", ex);
         }
      }
   }
}

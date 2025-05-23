using System.Security.Cryptography;

namespace MithrilShards.Chain.Bitcoin.Crypto
{
   /// <summary>
   /// Provides local hashing utility functions, primarily for HASH160 if the core version
   /// cannot be modified or uses a different RIPEMD160 implementation.
   /// </summary>
   public static class HashUtils
   {
      /// <summary>
      /// Computes HASH160 (SHA256 followed by RIPEMD160).
      /// Uses the locally available (potentially stubbed) RIPEMD160 implementation.
      /// </summary>
      /// <param name="data">The input data.</param>
      /// <returns>A 20-byte HASH160 result.</returns>
      public static byte[] Hash160(byte[] data)
      {
         byte[] sha256Hash;
         using (var sha256 = SHA256.Create())
         {
            sha256Hash = sha256.ComputeHash(data);
         }

         // Uses the RIPEMD160 class from MithrilShards.Chain.Bitcoin.Crypto,
         // which is currently stubbed.
         return RIPEMD160.ComputeHash(sha256Hash);
      }

      /// <summary>
      /// Computes SHA256(SHA256(data)).
      /// </summary>
      /// <param name="data">The input data.</param>
      /// <returns>A 32-byte HASH256 result.</returns>
      public static byte[] Hash256(byte[] data)
      {
         using (var sha256 = SHA256.Create())
         {
            return sha256.ComputeHash(sha256.ComputeHash(data));
         }
      }

      /// <summary>
      /// Computes a BIP340 tagged hash: SHA256(SHA256(tag) || SHA256(tag) || data).
      /// </summary>
      /// <param name="tag">The tag string.</param>
      /// <param name="data">The data to be hashed along with the tag.</param>
      /// <returns>A 32-byte tagged hash result.</returns>
      public static byte[] TaggedHash(string tag, byte[] data)
      {
         if (tag == null) throw new ArgumentNullException(nameof(tag));
         if (data == null) throw new ArgumentNullException(nameof(data));

         byte[] tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);
         byte[] hashedTag;

         using (var sha256 = SHA256.Create())
         {
            hashedTag = sha256.ComputeHash(tagBytes);

            // Concatenate hashedTag || hashedTag || data
            byte[] bytesToHash = new byte[hashedTag.Length * 2 + data.Length];
            Buffer.BlockCopy(hashedTag, 0, bytesToHash, 0, hashedTag.Length);
            Buffer.BlockCopy(hashedTag, 0, bytesToHash, hashedTag.Length, hashedTag.Length);
            Buffer.BlockCopy(data, 0, bytesToHash, hashedTag.Length * 2, data.Length);

            return sha256.ComputeHash(bytesToHash);
         }
      }
   }
}

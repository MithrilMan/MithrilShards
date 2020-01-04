using System;
using System.Security.Cryptography;

namespace MithrilShards.Core.Crypto
{
   public static class HashGenerator
   {
      public static ReadOnlySpan<byte> Sha256(ReadOnlySpan<byte> data)
      {
         using (var sha = new SHA256Managed())
         {
            return sha.ComputeHash(data.ToArray());
         }
      }

      public static ReadOnlySpan<byte> DoubleSha256(ReadOnlySpan<byte> data)
      {
         using (var sha = new SHA256Managed())
         {
            Span<byte> result = stackalloc byte[32];
            sha.TryComputeHash(data, result, out _);
            sha.TryComputeHash(result, result, out _);
            return result.ToArray();
         }
      }
   }
}

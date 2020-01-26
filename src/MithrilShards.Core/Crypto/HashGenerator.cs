using System;
using System.Security.Cryptography;

namespace MithrilShards.Core.Crypto
{
   public static partial class HashGenerator
   {
      public static ReadOnlySpan<byte> Sha256(ReadOnlySpan<byte> data)
      {
         using var sha = new SHA256Managed();
         Span<byte> result = new byte[32];

         if (!sha.TryComputeHash(data, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(Sha256)}");

         return result;
      }

      public static ReadOnlySpan<byte> DoubleSha256(ReadOnlySpan<byte> data)
      {
         using var sha = new SHA256Managed();
         Span<byte> result = new byte[32];

         if (!sha.TryComputeHash(data, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(Sha256)}");
         if (!sha.TryComputeHash(result, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(Sha256)}");

         return result;
      }

      public static ReadOnlySpan<byte> DoubleSha512AsBytes(ReadOnlySpan<byte> data)
      {
         using var sha = new SHA512Managed();
         Span<byte> result = new byte[64];
         sha.TryComputeHash(data, result, out _);
         sha.TryComputeHash(result, result, out _);
         return result.Slice(0, 32);
      }
   }
}

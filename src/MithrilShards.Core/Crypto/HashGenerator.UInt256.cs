using System;
using System.Security.Cryptography;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Core.Crypto;

public static partial class HashGenerator
{
   public static UInt256 DoubleSha256AsUInt256(ReadOnlySpan<byte> data)
   {
      using var sha = SHA256.Create();
      Span<byte> result = stackalloc byte[32];
      if (!sha.TryComputeHash(data, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(DoubleSha256AsUInt256)}");
      if (!sha.TryComputeHash(result, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(DoubleSha256AsUInt256)}");
      return new UInt256(result);
   }

   public static UInt256 DoubleSha512AsUInt256(ReadOnlySpan<byte> data)
   {
      using var sha = SHA512.Create();
      Span<byte> result = stackalloc byte[64];
      if (!sha.TryComputeHash(data, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(DoubleSha512AsUInt256)}");
      if (!sha.TryComputeHash(result, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(DoubleSha512AsUInt256)}");
      return new UInt256(result.Slice(0, 32));
   }
}

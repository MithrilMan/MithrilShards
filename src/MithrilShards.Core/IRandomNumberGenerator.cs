using System;

namespace MithrilShards.Core;

public interface IRandomNumberGenerator
{
   void GetBytes(Span<byte> data);

   void GetNonZeroBytes(Span<byte> data);

   int GetInt32();

   uint GetUint32();

   long GetInt64();

   ulong GetUint64();
}

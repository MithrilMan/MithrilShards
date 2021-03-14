using System;

namespace MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.NBitcoinTypes
{
   public static class Extensions
   {
		internal static byte[] SafeSubarray(this byte[] array, int offset, int count)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			if (offset < 0 || offset > array.Length)
				throw new ArgumentOutOfRangeException("offset");
			if (count < 0 || offset + count > array.Length)
				throw new ArgumentOutOfRangeException("count");
			if (offset == 0 && array.Length == count)
				return array;
			var data = new byte[count];
			Buffer.BlockCopy(array, offset, data, 0, count);
			return data;
		}
	}
}

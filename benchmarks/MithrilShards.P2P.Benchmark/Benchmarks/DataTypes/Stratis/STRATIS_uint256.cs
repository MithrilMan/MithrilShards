using System;
using System.Linq;

namespace MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Stratis;

public class STRATIS_uint256
{
   private const int WIDTH_BYTE = 256 / 8;
   internal readonly UInt32 pn0;
   internal readonly UInt32 pn1;
   internal readonly UInt32 pn2;
   internal readonly UInt32 pn3;
   internal readonly UInt32 pn4;
   internal readonly UInt32 pn5;
   internal readonly UInt32 pn6;
   internal readonly UInt32 pn7;


   public STRATIS_uint256(byte[] vch, bool lendian = true)
   {
      if (vch.Length != WIDTH_BYTE)
      {
         throw new FormatException("the byte array should be 256 byte long");
      }

      if (!lendian)
         vch = vch.Reverse().ToArray();

      pn0 = Utils.ToUInt32(vch, 4 * 0, true);
      pn1 = Utils.ToUInt32(vch, 4 * 1, true);
      pn2 = Utils.ToUInt32(vch, 4 * 2, true);
      pn3 = Utils.ToUInt32(vch, 4 * 3, true);
      pn4 = Utils.ToUInt32(vch, 4 * 4, true);
      pn5 = Utils.ToUInt32(vch, 4 * 5, true);
      pn6 = Utils.ToUInt32(vch, 4 * 6, true);
      pn7 = Utils.ToUInt32(vch, 4 * 7, true);

   }

   public STRATIS_uint256(byte[] vch)
       : this(vch, true)
   {
   }
}

public static class Utils
{
   public static uint ToUInt32(byte[] value, int index, bool littleEndian)
   {
      if (littleEndian)
      {
         return value[index]
                + ((uint)value[index + 1] << 8)
                + ((uint)value[index + 2] << 16)
                + ((uint)value[index + 3] << 24);
      }
      else
      {
         return value[index + 3]
                + ((uint)value[index + 2] << 8)
                + ((uint)value[index + 1] << 16)
                + ((uint)value[index + 0] << 24);
      }
   }
}

using System;

namespace MithrilShards.Core.DataTypes;

public partial class UInt256 : IEquatable<UInt256>
{
   public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as UInt256);

   public static bool operator !=(UInt256? a, UInt256? b) => !(a == b);

   public static bool operator ==(UInt256? a, UInt256? b) => ReferenceEquals(a, b) || (a?.Equals(b) ?? false);

   public bool Equals(UInt256? other)
   {
      if (other is null) return false;

      return part1 == other.part1
          && part2 == other.part2
          && part3 == other.part3
          && part4 == other.part4;
   }

   public static bool operator <(UInt256? a, UInt256? b)
   {
      return Compare(a, b) < 0;
   }

   public static bool operator >(UInt256? a, UInt256? b)
   {
      return Compare(a, b) > 0;
   }

   public static bool operator <=(UInt256? a, UInt256? b)
   {
      return Compare(a, b) <= 0;
   }

   public static bool operator >=(UInt256? a, UInt256? b)
   {
      return Compare(a, b) >= 0;
   }

   public static int Compare(UInt256? a, UInt256? b)
   {
      if (a is null) throw new ArgumentNullException(nameof(a));
      if (b is null) throw new ArgumentNullException(nameof(b));

      if (a.part4 < b.part4)
         return -1;
      if (a.part4 > b.part4)
         return 1;
      if (a.part3 < b.part3)
         return -1;
      if (a.part3 > b.part3)
         return 1;
      if (a.part2 < b.part2)
         return -1;
      if (a.part2 > b.part2)
         return 1;
      if (a.part1 < b.part1)
         return -1;
      if (a.part1 > b.part1)
         return 1;

      return 0;
   }
}

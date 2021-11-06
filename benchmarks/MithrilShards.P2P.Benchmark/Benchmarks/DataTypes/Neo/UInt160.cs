using System;
using System.Globalization;
using System.IO;

namespace MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Neo
{
   /// <summary>
   /// This class stores a 160 bit unsigned int, represented as a 20-byte little-endian byte array
   /// </summary>
   public class UInt160 : UIntBase, IComparable<UInt160>, IEquatable<UInt160>
   {
      public const int LENGTH = 20;
      public static readonly UInt160 Zero = new();

      private ulong _value1;
      private ulong _value2;
      private uint _value3;

      public override int Size => LENGTH;

      public UInt160()
      {
      }

      public unsafe UInt160(byte[] value)
      {
         fixed (ulong* p = &_value1)
         {
            var dst = new Span<byte>(p, LENGTH);
            value[..LENGTH].CopyTo(dst);
         }
      }

      /// <summary>
      /// Method CompareTo returns 1 if this UInt160 is bigger than other UInt160; -1 if it's smaller; 0 if it's equals
      /// Example: assume this is 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4, this.CompareTo(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) returns 1
      /// </summary>
      public int CompareTo(UInt160 other)
      {
         int result = _value3.CompareTo(other._value3);
         if (result != 0) return result;
         result = _value2.CompareTo(other._value2);
         if (result != 0) return result;
         return _value1.CompareTo(other._value1);
      }

      public override void Deserialize(BinaryReader reader)
      {
         _value1 = reader.ReadUInt64();
         _value2 = reader.ReadUInt64();
         _value3 = reader.ReadUInt32();
      }

      /// <summary>
      /// Method Equals returns true if objects are equal, false otherwise
      /// </summary>
      public override bool Equals(object obj)
      {
         if (ReferenceEquals(obj, this)) return true;
         return Equals(obj as UInt160);
      }

      /// <summary>
      /// Method Equals returns true if objects are equal, false otherwise
      /// </summary>
      public bool Equals(UInt160 other)
      {
         if (other is null) return false;
         return _value1 == other._value1
             && _value2 == other._value2
             && _value3 == other._value3;
      }

      public override int GetHashCode()
      {
         return (int)_value1;
      }

      /// <summary>
      /// Method Parse receives a big-endian hex string and stores as a UInt160 little-endian 20-bytes array
      /// Example: Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01") should create UInt160 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
      /// </summary>
      public static new UInt160 Parse(string value)
      {
         if (value == null)
            throw new ArgumentNullException(nameof(value));
         if (value.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            value = value.Substring(2);
         if (value.Length != LENGTH * 2)
            throw new FormatException();
         byte[] data = value.HexToBytes();
         Array.Reverse(data);
         return new UInt160(data);
      }

      public override void Serialize(BinaryWriter writer)
      {
         writer.Write(_value1);
         writer.Write(_value2);
         writer.Write(_value3);
      }

      /// <summary>
      /// Method TryParse tries to parse a big-endian hex string and store it as a UInt160 little-endian 20-bytes array
      /// Example: TryParse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01", result) should create result UInt160 01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
      /// </summary>
      public static bool TryParse(string s, out UInt160 result)
      {
         if (s == null)
         {
            result = null;
            return false;
         }
         if (s.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            s = s.Substring(2);
         if (s.Length != LENGTH * 2)
         {
            result = null;
            return false;
         }
         byte[] data = new byte[LENGTH];
         for (int i = 0; i < LENGTH; i++)
            if (!byte.TryParse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, null, out data[i]))
            {
               result = null;
               return false;
            }
         Array.Reverse(data);
         result = new UInt160(data);
         return true;
      }

      /// <summary>
      /// Returns true if left UInt160 is equals to right UInt160
      /// </summary>
      public static bool operator ==(UInt160 left, UInt160 right)
      {
         if (ReferenceEquals(left, right)) return true;
         if (left is null || right is null) return false;
         return left.Equals(right);
      }

      /// <summary>
      /// Returns true if left UIntBase is not equals to right UIntBase
      /// </summary>
      public static bool operator !=(UInt160 left, UInt160 right)
      {
         return !(left == right);
      }

      /// <summary>
      /// Operator > returns true if left UInt160 is bigger than right UInt160
      /// Example: UInt160(01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) > UInt160 (02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
      /// </summary>
      public static bool operator >(UInt160 left, UInt160 right)
      {
         return left.CompareTo(right) > 0;
      }

      /// <summary>
      /// Operator > returns true if left UInt160 is bigger or equals to right UInt160
      /// Example: UInt160(01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) >= UInt160 (02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
      /// </summary>
      public static bool operator >=(UInt160 left, UInt160 right)
      {
         return left.CompareTo(right) >= 0;
      }

      /// <summary>
      /// Operator > returns true if left UInt160 is less than right UInt160
      /// Example: UInt160(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) < UInt160 (01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
      /// </summary>
      public static bool operator <(UInt160 left, UInt160 right)
      {
         return left.CompareTo(right) < 0;
      }

      /// <summary>
      /// Operator > returns true if left UInt160 is less or equals to right UInt160
      /// Example: UInt160(02ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) < UInt160 (01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
      /// </summary>
      public static bool operator <=(UInt160 left, UInt160 right)
      {
         return left.CompareTo(right) <= 0;
      }
   }
}
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Neo;

/// <summary>
/// This class stores a 256 bit unsigned int, represented as a 32-byte little-endian byte array
/// </summary>
public class NEO_UInt256 : UIntBase, IComparable<NEO_UInt256>, IEquatable<NEO_UInt256>
{

   public const int LENGTH = 32;
   public static readonly NEO_UInt256 Zero = new();

   private ulong _value1;
   private ulong _value2;
   private ulong _value3;
   private ulong _value4;

   public override int Size => LENGTH;

   public NEO_UInt256()
   {
   }

   public unsafe NEO_UInt256(ReadOnlySpan<byte> value)
   {
      fixed (ulong* p = &_value1)
      {
         var dst = new Span<byte>(p, LENGTH);
         value[..LENGTH].CopyTo(dst);
      }
   }


   /// <summary>
   /// Method CompareTo returns 1 if this UInt256 is bigger than other UInt256; -1 if it's smaller; 0 if it's equals
   /// Example: assume this is 01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4, this.CompareTo(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) returns 1
   /// </summary>
   public int CompareTo(NEO_UInt256 other)
   {
      int result = _value4.CompareTo(other._value4);
      if (result != 0) return result;
      result = _value3.CompareTo(other._value3);
      if (result != 0) return result;
      result = _value2.CompareTo(other._value2);
      if (result != 0) return result;
      return _value1.CompareTo(other._value1);
   }

   public override void Deserialize(BinaryReader reader)
   {
      _value1 = reader.ReadUInt64();
      _value2 = reader.ReadUInt64();
      _value3 = reader.ReadUInt64();
      _value4 = reader.ReadUInt64();
   }

   /// <summary>
   /// Method Equals returns true if objects are equal, false otherwise
   /// </summary>
   public override bool Equals(object obj)
   {
      if (ReferenceEquals(obj, this)) return true;
      return Equals(obj as NEO_UInt256);
   }

   /// <summary>
   /// Method Equals returns true if objects are equal, false otherwise
   /// </summary>
   public bool Equals(NEO_UInt256 other)
   {
      if (other is null) return false;
      return _value1 == other._value1
          && _value2 == other._value2
          && _value3 == other._value3
          && _value4 == other._value4;
   }

   public override int GetHashCode()
   {
      return (int)_value1;
   }

   /// <summary>
   /// Method Parse receives a big-endian hex string and stores as a UInt256 little-endian 32-bytes array
   /// Example: Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01") should create UInt256 01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
   /// </summary>
   public static new NEO_UInt256 Parse(string s)
   {
      if (s == null)
         throw new ArgumentNullException();
      if (s.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
         s = s.Substring(2);
      if (s.Length != LENGTH * 2)
         throw new FormatException();
      byte[] data = s.HexToBytes();
      Array.Reverse(data);
      return new NEO_UInt256(data);
   }

   public override void Serialize(BinaryWriter writer)
   {
      writer.Write(_value1);
      writer.Write(_value2);
      writer.Write(_value3);
      writer.Write(_value4);
   }

   /// <summary>
   /// Method TryParse tries to parse a big-endian hex string and store it as a UInt256 little-endian 32-bytes array
   /// Example: TryParse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01", result) should create result UInt256 01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
   /// </summary>
   public static bool TryParse(string s, out NEO_UInt256 result)
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
      result = new NEO_UInt256(data);
      return true;
   }

   /// <summary>
   /// Returns true if left UInt256 is equals to right UInt256
   /// </summary>
   public static bool operator ==(NEO_UInt256 left, NEO_UInt256 right)
   {
      if (ReferenceEquals(left, right)) return true;
      if (left is null || right is null) return false;
      return left.Equals(right);
   }

   /// <summary>
   /// Returns true if left UIntBase is not equals to right UIntBase
   /// </summary>
   public static bool operator !=(NEO_UInt256 left, NEO_UInt256 right)
   {
      return !(left == right);
   }

   /// <summary>
   /// Operator > returns true if left UInt256 is bigger than right UInt256
   /// Example: UInt256(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) > UInt256(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
   /// </summary>
   public static bool operator >(NEO_UInt256 left, NEO_UInt256 right)
   {
      return left.CompareTo(right) > 0;
   }

   /// <summary>
   /// Operator >= returns true if left UInt256 is bigger or equals to right UInt256
   /// Example: UInt256(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) >= UInt256(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
   /// </summary>
   public static bool operator >=(NEO_UInt256 left, NEO_UInt256 right)
   {
      return left.CompareTo(right) >= 0;
   }

   /// <summary>
   /// Operator < returns true if left UInt256 is less than right UInt256
   /// Example: UInt256(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) < UInt256(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
   /// </summary>
   public static bool operator <(NEO_UInt256 left, NEO_UInt256 right)
   {
      return left.CompareTo(right) < 0;
   }

   /// <summary>
   /// Operator <= returns true if left UInt256 is less or equals to right UInt256
   /// Example: UInt256(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) <= UInt256(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
   /// </summary>
   public static bool operator <=(NEO_UInt256 left, NEO_UInt256 right)
   {
      return left.CompareTo(right) <= 0;
   }




   private static readonly NBitcoin.DataEncoders.HexEncoder _encoder = new();
   public override string ToString()
   {
      ulong[] arr = new ulong[] { _value1, _value2, _value3, _value4 };
      Span<byte> toBeReversed = MemoryMarshal.Cast<ulong, byte>(arr).ToArray();
      toBeReversed.Reverse();
      return _encoder.EncodeData(toBeReversed.ToArray());
   }
}

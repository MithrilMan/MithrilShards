﻿using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0009 // Member access should be qualified.

namespace MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Neo
{
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0011:Add braces", Justification = "<Pending>")]
   /// <summary>
   /// This class stores a 256 bit unsigned int, represented as a 32-byte little-endian byte array
   /// </summary>
   public class UInt256 : UIntBase, IComparable<UInt256>, IEquatable<UInt256>
   {

      public const int Length = 32;
      public static readonly UInt256 Zero = new UInt256();

      private ulong value1;
      private ulong value2;
      private ulong value3;
      private ulong value4;

      public override int Size => Length;

      public UInt256()
      {
      }

      public unsafe UInt256(ReadOnlySpan<byte> value)
      {
         fixed (ulong* p = &value1)
         {
            Span<byte> dst = new Span<byte>(p, Length);
            value[..Length].CopyTo(dst);
         }
      }


      /// <summary>
      /// Method CompareTo returns 1 if this UInt256 is bigger than other UInt256; -1 if it's smaller; 0 if it's equals
      /// Example: assume this is 01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4, this.CompareTo(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) returns 1
      /// </summary>
      public int CompareTo(UInt256 other)
      {
         int result = value4.CompareTo(other.value4);
         if (result != 0) return result;
         result = value3.CompareTo(other.value3);
         if (result != 0) return result;
         result = value2.CompareTo(other.value2);
         if (result != 0) return result;
         return value1.CompareTo(other.value1);
      }

      public override void Deserialize(BinaryReader reader)
      {
         value1 = reader.ReadUInt64();
         value2 = reader.ReadUInt64();
         value3 = reader.ReadUInt64();
         value4 = reader.ReadUInt64();
      }

      /// <summary>
      /// Method Equals returns true if objects are equal, false otherwise
      /// </summary>
      public override bool Equals(object obj)
      {
         if (ReferenceEquals(obj, this)) return true;
         return Equals(obj as UInt256);
      }

      /// <summary>
      /// Method Equals returns true if objects are equal, false otherwise
      /// </summary>
      public bool Equals(UInt256 other)
      {
         if (other is null) return false;
         return value1 == other.value1
             && value2 == other.value2
             && value3 == other.value3
             && value4 == other.value4;
      }

      public override int GetHashCode()
      {
         return (int)value1;
      }

      /// <summary>
      /// Method Parse receives a big-endian hex string and stores as a UInt256 little-endian 32-bytes array
      /// Example: Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01") should create UInt256 01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
      /// </summary>
      public static new UInt256 Parse(string s)
      {
         if (s == null)
            throw new ArgumentNullException();
         if (s.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            s = s.Substring(2);
         if (s.Length != Length * 2)
            throw new FormatException();
         byte[] data = s.HexToBytes();
         Array.Reverse(data);
         return new UInt256(data);
      }

      public override void Serialize(BinaryWriter writer)
      {
         writer.Write(value1);
         writer.Write(value2);
         writer.Write(value3);
         writer.Write(value4);
      }

      /// <summary>
      /// Method TryParse tries to parse a big-endian hex string and store it as a UInt256 little-endian 32-bytes array
      /// Example: TryParse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01", result) should create result UInt256 01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4
      /// </summary>
      public static bool TryParse(string s, out UInt256 result)
      {
         if (s == null)
         {
            result = null;
            return false;
         }
         if (s.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            s = s.Substring(2);
         if (s.Length != Length * 2)
         {
            result = null;
            return false;
         }
         byte[] data = new byte[Length];
         for (int i = 0; i < Length; i++)
            if (!byte.TryParse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, null, out data[i]))
            {
               result = null;
               return false;
            }
         Array.Reverse(data);
         result = new UInt256(data);
         return true;
      }

      /// <summary>
      /// Returns true if left UInt256 is equals to right UInt256
      /// </summary>
      public static bool operator ==(UInt256 left, UInt256 right)
      {
         if (ReferenceEquals(left, right)) return true;
         if (left is null || right is null) return false;
         return left.Equals(right);
      }

      /// <summary>
      /// Returns true if left UIntBase is not equals to right UIntBase
      /// </summary>
      public static bool operator !=(UInt256 left, UInt256 right)
      {
         return !(left == right);
      }

      /// <summary>
      /// Operator > returns true if left UInt256 is bigger than right UInt256
      /// Example: UInt256(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) > UInt256(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
      /// </summary>
      public static bool operator >(UInt256 left, UInt256 right)
      {
         return left.CompareTo(right) > 0;
      }

      /// <summary>
      /// Operator >= returns true if left UInt256 is bigger or equals to right UInt256
      /// Example: UInt256(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) >= UInt256(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) is true
      /// </summary>
      public static bool operator >=(UInt256 left, UInt256 right)
      {
         return left.CompareTo(right) >= 0;
      }

      /// <summary>
      /// Operator < returns true if left UInt256 is less than right UInt256
      /// Example: UInt256(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) < UInt256(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
      /// </summary>
      public static bool operator <(UInt256 left, UInt256 right)
      {
         return left.CompareTo(right) < 0;
      }

      /// <summary>
      /// Operator <= returns true if left UInt256 is less or equals to right UInt256
      /// Example: UInt256(02ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a3) <= UInt256(01ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00a4) is true
      /// </summary>
      public static bool operator <=(UInt256 left, UInt256 right)
      {
         return left.CompareTo(right) <= 0;
      }




      private static readonly NBitcoin.DataEncoders.HexEncoder Encoder = new NBitcoin.DataEncoders.HexEncoder();
      public override string ToString()
      {
         ulong[] arr = new ulong[] { this.value1, this.value2, this.value3, this.value4 };
         Span<byte> toBeReversed = MemoryMarshal.Cast<ulong, byte>(arr).ToArray();
         toBeReversed.Reverse();
         return Encoder.EncodeData(toBeReversed.ToArray());
      }
   }
}
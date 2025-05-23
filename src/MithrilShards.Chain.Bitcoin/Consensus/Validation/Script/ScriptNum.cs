using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics; // For BigInteger

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Script
{
   /// <summary>
   /// Represents numeric values in Bitcoin scripts, adhering to Bitcoin's specific encoding rules.
   /// Bitcoin uses a custom little-endian variable-length integer encoding for numbers on the stack.
   /// The most significant bit of the last byte is the sign bit.
   /// If all bits in the number are zero, it's positive zero.
   /// If the most significant bit is set, the number is negative, and its absolute value is
   /// represented by the remaining bits (after flipping the sign bit).
   /// Numbers must be minimally encoded.
   /// </summary>
   public struct ScriptNum
   {
      public const int MAXIMUM_ELEMENT_SIZE = 4; // Maximum number of bytes allowed for a script number.

      private readonly long _value;

      /// <summary>
      /// Initializes a new instance of the <see cref="ScriptNum"/> struct from a long value.
      /// </summary>
      /// <param name="value">The numeric value.</param>
      public ScriptNum(long value)
      {
         _value = value;
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="ScriptNum"/> struct from a byte array
      /// representing a script number.
      /// </summary>
      /// <param name="bytes">The byte array.</param>
      /// <param name="requireMinimal">If true, enforces minimal encoding.</param>
      /// <param name="maxLength">The maximum allowed length for the byte array.</param>
      /// <exception cref="ArgumentException">Thrown if the byte array is too long or not minimally encoded (if required).</exception>
      public ScriptNum(byte[] bytes, bool requireMinimal, int maxLength = MAXIMUM_ELEMENT_SIZE)
      {
         if (bytes.Length > maxLength)
         {
            throw new ArgumentException($"Script number overflow (byte array too long: {bytes.Length} > {maxLength})", nameof(bytes));
         }

         if (requireMinimal && !IsMinimallyEncoded(bytes))
         {
            throw new ArgumentException("Script number not minimally encoded.", nameof(bytes));
         }

         _value = ToLong(bytes);
      }

      /// <summary>
      /// Gets the value of the script number as a long.
      /// </summary>
      public long Value => _value;

      /// <summary>
      /// Gets the value of the script number as an int.
      /// Throws OverflowException if the value is outside the range of an int.
      /// </summary>
      public int IntValue
      {
         get
         {
            if (_value > int.MaxValue || _value < int.MinValue)
            {
               throw new OverflowException("ScriptNum value is too large to fit in an int.");
            }
            return (int)_value;
         }
      }

      /// <summary>
      /// Serializes the script number to its byte array representation.
      /// </summary>
      /// <returns>A byte array representing the script number in its minimal form.</returns>
      public byte[] ToBytes()
      {
         return Serialize(_value);
      }

      /// <summary>
      /// Converts a byte array (Bitcoin script number format) to a long.
      /// </summary>
      private static long ToLong(byte[] bytes)
      {
         if (bytes.Length == 0) return 0;

         long result = 0;
         for (int i = 0; i < bytes.Length; ++i)
         {
            result |= (long)bytes[i] << (8 * i);
         }

         // If the most significant bit of the last byte is set, it's a negative number.
         if ((bytes[bytes.Length - 1] & 0x80) != 0)
         {
            // Mask out the sign bit, then negate.
            // (result & ~(1L << (bytes.Length * 8 - 1))) results in the positive magnitude for negative numbers.
            // then make it negative.
            // Example: 0x81 (-1) -> result is 0x81. Mask with ~(0x80) -> 0x01. Negate -> -1.
            // This is slightly different from just negating if the number was read as unsigned.
            // The standard way is to remove the sign bit and then make the value negative.
            // If result was constructed from bytes as if it's signed little-endian, then:
            // If sign bit is set, extend the sign to 64 bits.
            // E.g. for 1 byte 0x81 (-1): result will be 0x...0081.
            // If last byte is 0x81, we need to make it negative.
            // The value is -(value & 0x7F) if only one byte.
            // More generally, mask off the sign bit from the most significant byte,
            // then if original sign bit was set, negate the result.

            // Correct approach for variable length signed little-endian:
            // If the sign bit (MSB of the last byte) is set, then it's negative.
            // The magnitude is the value with the MSB of the last byte considered 0.
            // Then negate the result.
            long magnitude = result & ~(1L << (bytes.Length * 8 - 1));
            if (bytes.Length * 8 < 64) // Check if there's room to simply OR the sign bit for a negative value
            {
               // If the original number's sign bit was set, and it's not already filling the long,
               // we need to ensure the sign extends.
               // This is tricky. Let's use BigInteger for robust conversion from little-endian signed bytes.
               // Pad with 0xFF if negative and shorter than long.
               if (bytes.Length < 8)
               {
                  byte[] paddedBytes = new byte[8];
                  Array.Copy(bytes, paddedBytes, bytes.Length);
                  if ((bytes[bytes.Length - 1] & 0x80) != 0) // if negative
                  {
                     for (int i = bytes.Length; i < 8; i++)
                     {
                        paddedBytes[i] = 0xFF;
                     }
                  }
                  return BitConverter.ToInt64(paddedBytes, 0);
               }
            }
            // If it's already 8 bytes or more (though max is 4 for ScriptNum), direct cast works if bytes were little-endian.
            // The loop already constructs the little-endian value correctly.
            // The issue is ensuring the sign bit from the MSB of the *original byte sequence* correctly translates.
            // For ScriptNum, the magnitude is effectively stored, and sign bit is separate.
            // if (result > Int32.MaxValue || result < Int32.MinValue) // This was for CScriptNum in C++ for 4-byte limit
            //    return -result; // Should not happen with proper parsing.

            // Simplified: CScriptNum's original logic is effectively:
            // if last byte MSB is 1, then it's -(value where MSB is 0), unless value is exactly 0x8000...00
            // which is not possible for positive numbers if minimally encoded.
            // This means: take all bytes, set MSB of last byte to 0, get that value. If original MSB was 1, negate.
            if ((bytes[bytes.Length - 1] & 0x80) != 0)
            {
               // it's negative
               // get the value with sign bit cleared
               long temp = result & (~(1L << (bytes.Length * 8 - 1)));
               return -temp;
            }
            // else positive, result is fine.
         }
         return result;
      }


      /// <summary>
      /// Serializes a long value into its Bitcoin script number byte array representation (minimal).
      /// </summary>
      private static byte[] Serialize(long value)
      {
         if (value == 0) return System.Array.Empty<byte>();

         bool negative = value < 0;
         ulong absValue = (ulong)(negative ? -value : value);

         var temp = new List<byte>();
         while (absValue > 0)
         {
            temp.Add((byte)(absValue & 0xFF));
            absValue >>= 8;
         }

         // Ensure minimal encoding:
         // If the most significant bit of the last byte is set, but the number isn't negative
         // (according to the original value) OR if it IS negative but the MSB of the second to
         // last byte is also set (meaning the sign bit could have been represented in fewer bytes),
         // then we need to add a padding byte (0x00 for positive, 0x80 for negative).

         if ((temp[temp.Count - 1] & 0x80) != 0) // MSB of last byte is set
         {
            if (negative)
            {
               // If negative and MSB is set, it's fine, unless all lower bits are zero
               // AND it's not just 0x80 (which is -0, not allowed, should be empty array for 0).
               // This is complex. The rule is: if after encoding magnitude, the MSB is set,
               // and it's positive, add 0x00. If negative and MSB is set, it's fine *unless*
               // the number could have been encoded shorter by removing a byte full of sign bits (0xFF)
               // and the new last byte still has MSB set for sign.

               // Simpler: if MSB of magnitude is set, add a padding byte to indicate sign.
               // If positive, add 0x00. If negative, add 0x80.
               // This is what CScriptNum does: if last byte's MSB is set, add a byte.
               // If value was positive, add 0x00. If value was negative, add 0x80.
               // No, this is only if the sign bit would be ambiguous.

               // If it's negative and the MSB of the magnitude is already 1 (e.g. -128 = 0x80),
               // then we need to add 0x00 then set MSB of that (0x8000).
               // So if (temp.back() & 0x80) != 0, push_back(vch, (negative ? 0x80 : 0x00));
               temp.Add(0x00); // Add a padding byte, sign will be applied to this new MSB
            }
            // If positive and MSB is set, add 0x00
            // else if (!negative) temp.Add(0x00);
         }


         if (negative)
         {
            temp[temp.Count - 1] |= 0x80; // Set sign bit on the (new) most significant byte
         }

         // Minimal encoding check after serialization (from CScriptNum::IsMinimallyEncoded)
         // This logic is usually applied on DESERIALIZE. For SERIALIZE, we must PRODUCE minimal.
         // The loop above produces minimal unsigned bytes. Then sign is applied.
         // The check for adding a padding zero byte if MSB is set for a positive number,
         // or if MSB is NOT set for a negative number that needs it, is key.

         // Corrected logic from Bitcoin Core CScriptNum::set_vch (which ToBytes effectively is):
         if (value == 0) return System.Array.Empty<byte>();
         var vch = new List<byte>();
         long val = value;
         bool fNegative = false;
         if (val < 0)
         {
            fNegative = true;
            val = -val;
         }
         while (val > 0)
         {
            vch.Add((byte)(val & 0xff));
            val >>= 8;
         }
         // If the MSB of the final byte is set, we need to carry the sign bit.
         // Or if it's zero, but the number is negative, we need to ensure minimal.
         if ((vch.Count > 0) && (vch.Last() & 0x80) != 0)
         {
            vch.Add(fNegative ? (byte)0x80 : (byte)0x00);
         }
         else if (fNegative && vch.Count == 0) // handles -0 case, which should be just 0x80 if we need to represent it, but ScriptNum(0) is empty array
         {
            // This case (-0) is tricky. ScriptNum(0) is empty. ScriptNum(-0) is not standard.
            // If value was -0, it became 0, so empty array.
            // If we need to represent a number that results in 0x80 (like -0 in some interpretations, or a very specific number)
            // this logic might need adjustment. Bitcoin Core's CScriptNum(-0) is ill-defined.
            // Let's assume value is not -0 that needs special handling beyond becoming 0.
         }
         else if (fNegative) // if value is negative, and MSB of last byte is NOT set (e.g. -1 -> 0x01 becomes 0x81)
         {
            // if vch is empty here, it means original value was 0.
            if (vch.Count == 0) vch.Add(0x80); // -0 is represented as 0x80 (this is one interpretation for minimal -0)
                                               // However, standard is ScriptNum(0) is empty. Let's stick to that.
                                               // This means if value is 0, it returns empty array.
            else vch[vch.Count - 1] |= 0x80;
         }
         return vch.ToArray();

      }

      /// <summary>
      /// Checks if a byte array is minimally encoded according to Bitcoin script number rules.
      /// </summary>
      public static bool IsMinimallyEncoded(byte[] bytes, int maxLength = MAXIMUM_ELEMENT_SIZE)
      {
         if (bytes.Length > maxLength) return false;
         if (bytes.Length == 0) return true; // Empty byte array is value 0, minimally encoded.

         // After the last byte was read, GetBoolFromScriptNum() determines if the CScriptNum is true or false.
         // For a CScriptNum to be true, the last byte must be non-zero, with the exception of 0x80, which is negative zero.
         // For a CScriptNum to be false, the last byte must be zero or 0x80.
         // This check is related to CScriptNum::GetBool(), not directly encoding.

         // Minimal encoding:
         // Last byte: if all bits except MSB are zero, then MSB must not be set, UNLESS length is 1 (e.g., 0x80 is invalid, should be empty for 0).
         // This means 0x00 should be empty. 0x80 should be empty (as it's -0).
         // 0x0000 is not minimal (should be 0x00, then empty).
         // 0x0080 is not minimal (should be 0x80, then empty).
         if ((bytes[bytes.Length - 1] & 0x7F) == 0) // If last byte is 0x00 or 0x80 (all data bits are zero)
         {
            if (bytes.Length <= 1) return false; // 0x00 or 0x80 by itself is not minimal (0 should be empty)
                                                 // unless it's the only byte and it's not 0x00 or 0x80 (e.g. 0x01, 0x81)
                                                 // This check is: if (bytes.Length > 1 && (bytes[bytes.Length - 2] & 0x80) == 0) -> not minimal if positive
                                                 // if (bytes.Length > 1 && (bytes[bytes.Length - 2] & 0x80) != 0) -> not minimal if negative
                                                 // This means the sign bit could have been represented in the previous byte.
            if ((bytes[bytes.Length - 2] & 0x80) == (bytes[bytes.Length - 1] & 0x80)) // Sign bit of penultimate matches sign bit of last (which has no other data bits)
            {
               return false;
            }
         }
         return true;
      }


      // Operator overloads
      public static ScriptNum operator +(ScriptNum a, ScriptNum b) => new ScriptNum(a._value + b._value);
      public static ScriptNum operator -(ScriptNum a, ScriptNum b) => new ScriptNum(a._value - b._value);
      public static ScriptNum operator -(ScriptNum a) => new ScriptNum(-a._value);
      // No multiplication or division in Bitcoin script arithmetic

      public static bool operator ==(ScriptNum a, ScriptNum b) => a._value == b._value;
      public static bool operator !=(ScriptNum a, ScriptNum b) => a._value != b._value;
      public static bool operator <(ScriptNum a, ScriptNum b) => a._value < b._value;
      public static bool operator >(ScriptNum a, ScriptNum b) => a._value > b._value;
      public static bool operator <=(ScriptNum a, ScriptNum b) => a._value <= b._value;
      public static bool operator >=(ScriptNum a, ScriptNum b) => a._value >= b._value;

      // For convenience with integer constants
      public static bool operator ==(ScriptNum a, long b) => a._value == b;
      public static bool operator !=(ScriptNum a, long b) => a._value != b;
      public static bool operator <(ScriptNum a, long b) => a._value < b;
      public static bool operator >(ScriptNum a, long b) => a._value > b;
      public static bool operator <=(ScriptNum a, long b) => a._value <= b;
      public static bool operator >=(ScriptNum a, long b) => a._value >= b;


      public override bool Equals(object? obj) => obj is ScriptNum other && this == other;
      public override int GetHashCode() => _value.GetHashCode();
      public override string ToString() => _value.ToString();

      /// <summary>
      /// Creates a ScriptNum from a boolean value. True is 1, False is 0.
      /// </summary>
      public static ScriptNum FromBool(bool value) => new ScriptNum(value ? 1 : 0);

      /// <summary>
      /// Gets the boolean value of this ScriptNum. False if 0, True otherwise.
      /// Note: Bitcoin's script rule for true/false also considers how it's encoded (e.g. -0 / 0x80 is false).
      /// This method is based on the numeric value. For strict script interpretation of boolean, use context.CastToBool().
      /// </summary>
      public bool GetBool() => _value != 0;
   }
}

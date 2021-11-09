using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;

namespace MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.NBitcoinTypes;

/// <summary>
/// Represent the challenge that miners must solve for finding a new block
/// </summary>
public class NBitcoin_Target
{
   static NBitcoin_Target _difficulty1 = new NBitcoin_Target(new byte[] { 0x1d, 0x00, 0xff, 0xff });
   public static NBitcoin_Target Difficulty1
   {
      get
      {
         return _difficulty1;
      }
   }

   public NBitcoin_Target(uint compact)
      : this(ToBytes(compact))
   {

   }

   private static byte[] ToBytes(uint bits)
   {
      return new byte[]
      {
            (byte)(bits >> 24),
            (byte)(bits >> 16),
            (byte)(bits >> 8),
            (byte)(bits)
      };
   }



   BigInteger _target;

   public NBitcoin_Target(byte[] compact)
   {
      if (compact.Length == 4)
      {
         var exp = compact[0];
         var val = new BigInteger(compact.SafeSubarray(1, 3));
         _target = val.ShiftLeft(8 * (exp - 3));
      }
      else
         throw new FormatException("Invalid number of bytes");
   }

   public NBitcoin_Target(BigInteger target)
   {
      _target = target;
      _target = new NBitcoin_Target(ToCompact())._target;
   }
   public NBitcoin_Target(uint256 target)
   {
      _target = new BigInteger(target.ToBytes(false));
      _target = new NBitcoin_Target(ToCompact())._target;
   }

   public static implicit operator NBitcoin_Target(uint a)
   {
      return new NBitcoin_Target(a);
   }
   public static implicit operator uint(NBitcoin_Target a)
   {
      var bytes = a._target.ToByteArray();
      var val = bytes.SafeSubarray(0, Math.Min(bytes.Length, 3));
      Array.Reverse(val);
      var exp = (byte)(bytes.Length);
      if (exp == 1 && bytes[0] == 0)
         exp = 0;
      var missing = 4 - val.Length;
      if (missing > 0)
         val = val.Concat(new byte[missing]).ToArray();
      if (missing < 0)
         val = val.Take(-missing).ToArray();
      return (uint)val[0] + (uint)(val[1] << 8) + (uint)(val[2] << 16) + (uint)(exp << 24);
   }

   double? _difficulty;

   public double Difficulty
   {
      get
      {
         if (_difficulty == null)
         {
            var qr = Difficulty1._target.DivideAndRemainder(_target);
            var quotient = qr[0];
            var remainder = qr[1];
            var decimalPart = BigInteger.Zero;

            var quotientStr = quotient.ToString();
            int precision = 12;
            var builder = new StringBuilder(quotientStr.Length + 1 + precision);
            builder.Append(quotientStr);
            builder.Append('.');
            for (int i = 0; i < precision; i++)
            {
               var div = (remainder.Multiply(BigInteger.Ten)).Divide(_target);
               decimalPart = decimalPart.Multiply(BigInteger.Ten);
               decimalPart = decimalPart.Add(div);

               remainder = remainder.Multiply(BigInteger.Ten).Subtract(div.Multiply(_target));
            }
            builder.Append(decimalPart.ToString().PadLeft(precision, '0'));
            _difficulty = double.Parse(builder.ToString(), new NumberFormatInfo()
            {
               NegativeSign = "-",
               NumberDecimalSeparator = "."
            });
         }
         return _difficulty.Value;
      }
   }



   public override bool Equals(object obj)
   {
      var item = obj as NBitcoin_Target;
      if (item == null)
         return false;
      return _target.Equals(item._target);
   }
   public static bool operator ==(NBitcoin_Target a, NBitcoin_Target b)
   {
      if (System.Object.ReferenceEquals(a, b))
         return true;
      if (((object)a == null) || ((object)b == null))
         return false;
      return a._target.Equals(b._target);
   }

   public static bool operator !=(NBitcoin_Target a, NBitcoin_Target b)
   {
      return !(a == b);
   }

   public override int GetHashCode()
   {
      return _target.GetHashCode();
   }

   public BigInteger ToBigInteger()
   {
      return _target;
   }

   public uint ToCompact()
   {
      return (uint)this;
   }

   public uint256 ToUInt256()
   {
      return ToUInt256(_target);
   }

   internal static uint256 ToUInt256(BigInteger input)
   {
      var array = input.ToByteArray();

      var missingZero = 32 - array.Length;
      if (missingZero < 0)
         throw new InvalidOperationException("Awful bug, this should never happen");
      if (missingZero != 0)
      {
         array = new byte[missingZero].Concat(array).ToArray();
      }
      return new uint256(array, false);
   }

   public override string ToString()
   {
      return ToUInt256().ToString();
   }
}

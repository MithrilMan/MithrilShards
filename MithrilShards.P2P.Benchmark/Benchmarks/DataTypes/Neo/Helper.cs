﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace MithrilShards.P2P.Benchmark.Benchmarks.DataTypes.Neo
{
   public static class Helper
   {
      private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

      internal static byte[] ToArray(this SecureString s)
      {
         if (s == null)
            throw new NullReferenceException();
         if (s.Length == 0)
            return Array.Empty<byte>();
         var result = new List<byte>();
         IntPtr ptr = SecureStringMarshal.SecureStringToGlobalAllocAnsi(s);
         try
         {
            int i = 0;
            do
            {
               byte b = Marshal.ReadByte(ptr, i++);
               if (b == 0)
                  break;
               result.Add(b);
            } while (true);
         }
         finally
         {
            Marshal.ZeroFreeGlobalAllocAnsi(ptr);
         }
         return result.ToArray();
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private static int BitLen(int w)
      {
         return (w < 1 << 15 ? (w < 1 << 7
             ? (w < 1 << 3 ? (w < 1 << 1
             ? (w < 1 << 0 ? (w < 0 ? 32 : 0) : 1)
             : (w < 1 << 2 ? 2 : 3)) : (w < 1 << 5
             ? (w < 1 << 4 ? 4 : 5)
             : (w < 1 << 6 ? 6 : 7)))
             : (w < 1 << 11
             ? (w < 1 << 9 ? (w < 1 << 8 ? 8 : 9) : (w < 1 << 10 ? 10 : 11))
             : (w < 1 << 13 ? (w < 1 << 12 ? 12 : 13) : (w < 1 << 14 ? 14 : 15)))) : (w < 1 << 23 ? (w < 1 << 19
             ? (w < 1 << 17 ? (w < 1 << 16 ? 16 : 17) : (w < 1 << 18 ? 18 : 19))
             : (w < 1 << 21 ? (w < 1 << 20 ? 20 : 21) : (w < 1 << 22 ? 22 : 23))) : (w < 1 << 27
             ? (w < 1 << 25 ? (w < 1 << 24 ? 24 : 25) : (w < 1 << 26 ? 26 : 27))
             : (w < 1 << 29 ? (w < 1 << 28 ? 28 : 29) : (w < 1 << 30 ? 30 : 31)))));
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static byte[] Concat(params byte[][] buffers)
      {
         int length = 0;
         for (int i = 0; i < buffers.Length; i++)
            length += buffers[i].Length;
         byte[] dst = new byte[length];
         int p = 0;
         foreach (byte[] src in buffers)
         {
            Buffer.BlockCopy(src, 0, dst, p, src.Length);
            p += src.Length;
         }
         return dst;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      internal static int GetBitLength(this BigInteger i)
      {
         byte[] b = i.ToByteArray();
         return (b.Length - 1) * 8 + BitLen(i.Sign > 0 ? b[b.Length - 1] : 255 - b[b.Length - 1]);
      }

      internal static int GetLowestSetBit(this BigInteger i)
      {
         if (i.Sign == 0)
            return -1;
         byte[] b = i.ToByteArray();
         int w = 0;
         while (b[w] == 0)
            w++;
         for (int x = 0; x < 8; x++)
            if ((b[w] & 1 << x) > 0)
               return x + w * 8;
         throw new Exception();
      }

      internal static void Remove<T>(this HashSet<T> set, ISet<T> other)
      {
         if (set.Count > other.Count)
         {
            set.ExceptWith(other);
         }
         else
         {
            set.RemoveWhere(u => other.Contains(u));
         }
      }


      internal static void Remove<T, V>(this HashSet<T> set, IReadOnlyDictionary<T, V> other)
      {
         if (set.Count > other.Count)
         {
            set.ExceptWith(other.Keys);
         }
         else
         {
            set.RemoveWhere(u => other.ContainsKey(u));
         }
      }


      public static byte[] HexToBytes(this string value)
      {
         if (value == null || value.Length == 0)
            return Array.Empty<byte>();
         if (value.Length % 2 == 1)
            throw new FormatException();
         byte[] result = new byte[value.Length / 2];
         for (int i = 0; i < result.Length; i++)
            result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
         return result;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      internal static BigInteger Mod(this BigInteger x, BigInteger y)
      {
         x %= y;
         if (x.Sign < 0)
            x += y;
         return x;
      }

      internal static BigInteger ModInverse(this BigInteger a, BigInteger n)
      {
         BigInteger i = n, v = 0, d = 1;
         while (a > 0)
         {
            BigInteger t = i / a, x = a;
            a = i % x;
            i = x;
            x = d;
            d = v - t * x;
            v = x;
         }
         v %= n;
         if (v < 0) v = (v + n) % n;
         return v;
      }

      internal static BigInteger NextBigInteger(this Random rand, int sizeInBits)
      {
         if (sizeInBits < 0)
            throw new ArgumentException("sizeInBits must be non-negative");
         if (sizeInBits == 0)
            return 0;
         Span<byte> b = stackalloc byte[sizeInBits / 8 + 1];
         rand.NextBytes(b);
         if (sizeInBits % 8 == 0)
            b[b.Length - 1] = 0;
         else
            b[b.Length - 1] &= (byte)((1 << sizeInBits % 8) - 1);
         return new BigInteger(b);
      }

      internal static BigInteger NextBigInteger(this RandomNumberGenerator rng, int sizeInBits)
      {
         if (sizeInBits < 0)
            throw new ArgumentException("sizeInBits must be non-negative");
         if (sizeInBits == 0)
            return 0;
         Span<byte> b = stackalloc byte[sizeInBits / 8 + 1];
         rng.GetBytes(b);
         if (sizeInBits % 8 == 0)
            b[b.Length - 1] = 0;
         else
            b[b.Length - 1] &= (byte)((1 << sizeInBits % 8) - 1);
         return new BigInteger(b);
      }

      public static BigInteger Sum(this IEnumerable<BigInteger> source)
      {
         var sum = BigInteger.Zero;
         foreach (var bi in source) sum += bi;
         return sum;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      internal static bool TestBit(this BigInteger i, int index)
      {
         return (i & (BigInteger.One << index)) > BigInteger.Zero;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static byte[] ToByteArrayStandard(this BigInteger i)
      {
         if (i.IsZero) return Array.Empty<byte>();
         return i.ToByteArray();
      }

      public static string ToHexString(this byte[] value)
      {
         var sb = new StringBuilder();
         foreach (byte b in value)
            sb.AppendFormat("{0:x2}", b);
         return sb.ToString();
      }

      public static string ToHexString(this byte[] value, bool reverse = false)
      {
         var sb = new StringBuilder();
         for (int i = 0; i < value.Length; i++)
            sb.AppendFormat("{0:x2}", value[reverse ? value.Length - i - 1 : i]);
         return sb.ToString();
      }

      public static string ToHexString(this ReadOnlySpan<byte> value)
      {
         var sb = new StringBuilder();
         foreach (byte b in value)
            sb.AppendFormat("{0:x2}", b);
         return sb.ToString();
      }

      public static uint ToTimestamp(this DateTime time)
      {
         return (uint)(time.ToUniversalTime() - _unixEpoch).TotalSeconds;
      }

      public static ulong ToTimestampMS(this DateTime time)
      {
         return (ulong)(time.ToUniversalTime() - _unixEpoch).TotalMilliseconds;
      }

      /// <summary>
      /// Checks if address is IPv4 Maped to IPv6 format, if so, Map to IPv4.
      /// Otherwise, return current address.
      /// </summary>
      internal static IPAddress Unmap(this IPAddress address)
      {
         if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
         return address;
      }

      /// <summary>
      /// Checks if IPEndPoint is IPv4 Maped to IPv6 format, if so, unmap to IPv4.
      /// Otherwise, return current endpoint.
      /// </summary>
      internal static IPEndPoint Unmap(this IPEndPoint endPoint)
      {
         if (!endPoint.Address.IsIPv4MappedToIPv6)
            return endPoint;
         return new IPEndPoint(endPoint.Address.Unmap(), endPoint.Port);
      }

      internal static BigInteger WeightedAverage<T>(this IEnumerable<T> source, Func<T, BigInteger> valueSelector, Func<T, BigInteger> weightSelector)
      {
         BigInteger sum_weight = BigInteger.Zero;
         BigInteger sum_value = BigInteger.Zero;
         foreach (T item in source)
         {
            BigInteger weight = weightSelector(item);
            sum_weight += weight;
            sum_value += valueSelector(item) * weight;
         }
         if (sum_value == BigInteger.Zero) return BigInteger.Zero;
         return sum_value / sum_weight;
      }

      internal static IEnumerable<TResult> WeightedFilter<T, TResult>(this IList<T> source, double start, double end, Func<T, BigInteger> weightSelector, Func<T, BigInteger, TResult> resultSelector)
      {
         if (source == null) throw new ArgumentNullException(nameof(source));
         if (start < 0 || start > 1) throw new ArgumentOutOfRangeException(nameof(start));
         if (end < start || start + end > 1) throw new ArgumentOutOfRangeException(nameof(end));
         if (weightSelector == null) throw new ArgumentNullException(nameof(weightSelector));
         if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
         if (source.Count == 0 || start == end) yield break;
         double amount = (double)source.Select(weightSelector).Sum();
         BigInteger sum = 0;
         double current = 0;
         foreach (T item in source)
         {
            if (current >= end) break;
            BigInteger weight = weightSelector(item);
            sum += weight;
            double old = current;
            current = (double)sum / amount;
            if (current <= start) continue;
            if (old < start)
            {
               if (current > end)
               {
                  weight = (long)((end - start) * amount);
               }
               else
               {
                  weight = (long)((current - start) * amount);
               }
            }
            else if (current > end)
            {
               weight = (long)((end - old) * amount);
            }
            yield return resultSelector(item, weight);
         }
      }
   }
}

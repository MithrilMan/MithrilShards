using System;
using System.Net;
using System.Net.Sockets;

namespace MithrilShards.Core.Extensions;

public static class IPAddressExtensions
{
   public static bool IsAnyIP(this IPAddress address)
   {
      if (address.AddressFamily == AddressFamily.InterNetwork)
      {
         return address.Equals(IPAddress.Parse("0.0.0.0"));
      }
      else if (address.AddressFamily == AddressFamily.InterNetworkV6)
      {
         if (address.IsIPv4MappedToIPv6)
         {
            return address.Equals(IPAddress.Parse("0.0.0.0"));
         }
         else
         {
            return address.Equals(IPAddress.Parse("[::]"));
         }
      }
      else
      {
         throw new Exception("Unexpected address family");
      }
   }

   /// <summary>
   /// Specific IP address ranges that are reserved specifically as non - routable addresses to be used in
   /// private networks: 10.0.0.0 through 10.255.255.255. 172.16.0.0 through 172.32.255.255. 192.168.0.0
   /// through 192.168.255.255.
   /// </summary>
   public static bool IsRoutable(this IPAddress address, bool allowLocal)
   {
      address = address.MapToIPv6();
      ReadOnlySpan<byte> addressBytes = address.GetAddressBytes();
      bool isIPv4 = address.IsIPv4MappedToIPv6;

      return
         addressBytes.IsValid(isIPv4) && !(
            (!allowLocal && addressBytes.IsRFC1918(isIPv4))
            || addressBytes.IsRFC6890(isIPv4)
            || addressBytes.IsRFC5737(isIPv4)
            || addressBytes.IsRFC6598(isIPv4)
            || addressBytes.IsRFC7534(isIPv4)
            || addressBytes.IsRFC2544(isIPv4)
            || addressBytes.IsRFC1112(isIPv4)
            || addressBytes.IsRFC3927(isIPv4)
            || addressBytes.IsRFC4862()
            || (addressBytes.IsRFC4193() && !addressBytes.IsTor())
            || addressBytes.IsRFC4843()
            || (!allowLocal && IPAddress.IsLoopback(address))
            );
   }



   private static bool IsRFC1918(this ref ReadOnlySpan<byte> bytes, bool isIPv4)
   {
      return isIPv4 && (
          bytes[15 - 3] == 10 ||
          (bytes[15 - 3] == 192 && bytes[15 - 2] == 168) ||
          (bytes[15 - 3] == 172 && (bytes[15 - 2] >= 16 && bytes[15 - 2] <= 31)));
   }

   /// <summary>192.0.0.0/24 IETF Protocol Assignments.</summary>
   private static bool IsRFC6890(this ref ReadOnlySpan<byte> bytes, bool isIPv4)
   {
      return isIPv4 && (
          bytes[15 - 3] == 192 && bytes[15 - 2] == 0 && bytes[15 - 1] == 0);
   }

   /// <summary>192.0.2.0/24, 198.51.100.0/24, 203.0.113.0/24 Documentation. Not globally reachable.</summary>
   private static bool IsRFC5737(this ref ReadOnlySpan<byte> bytes, bool isIPv4)
   {
      return isIPv4 && (
          (bytes[15 - 3] == 192 && bytes[15 - 2] == 0 && bytes[15 - 1] == 2) ||
          (bytes[15 - 3] == 198 && bytes[15 - 2] == 51 && bytes[15 - 1] == 100) ||
          (bytes[15 - 3] == 203 && bytes[15 - 2] == 0 && bytes[15 - 1] == 113));
   }

   /// <summary>100.64.0.0/10 Shared Address Space. Not globally reachable.</summary>
   private static bool IsRFC6598(this ref ReadOnlySpan<byte> bytes, bool isIPv4)
   {
      return isIPv4 && (
          bytes[15 - 3] == 100 && (bytes[15 - 2] & 0xc0) == 64);
   }

   /// <summary>192.175.48.0/24 Direct Delegation AS112 Service. Globally reachable. Not globally unique.</summary>
   private static bool IsRFC7534(this ref ReadOnlySpan<byte> bytes, bool isIPv4)
   {
      return isIPv4 && (
          bytes[15 - 3] == 192 && bytes[15 - 2] == 175 && bytes[15 - 1] == 48);
   }

   /// <summary>198.18.0.0/15 Benchmarking. Not globally reachable.</summary>
   private static bool IsRFC2544(this ref ReadOnlySpan<byte> bytes, bool isIPv4)
   {
      return isIPv4 && (
          bytes[15 - 3] == 198 && (bytes[15 - 2] & 254) == 18);
   }

   /// <summary>240.0.0.0/4 Reserved.</summary>
   private static bool IsRFC1112(this ref ReadOnlySpan<byte> bytes, bool isIPv4)
   {
      return isIPv4 && (
          (bytes[15 - 3] & 240) == 240);
   }

   private static bool IsRFC3927(this ref ReadOnlySpan<byte> bytes, bool isIPv4)
   {
      return isIPv4 && (bytes[15 - 3] == 169 && bytes[15 - 2] == 254);
   }

   private static readonly byte[] _pchRFC4862 = new byte[] { 0xFE, 0x80, 0, 0, 0, 0, 0, 0 };
   private static bool IsRFC4862(this ref ReadOnlySpan<byte> bytes)
   {
      //TODO: test correctness of my changes
      return bytes.Slice(0, _pchRFC4862.Length).SequenceEqual(_pchRFC4862);
   }

   private static bool IsRFC4193(this ReadOnlySpan<byte> bytes)
   {
      return (bytes[15 - 15] & 0xFE) == 0xFC;
   }

   private static readonly byte[] _pchOnionCat = new byte[] { 0xFD, 0x87, 0xD8, 0x7E, 0xEB, 0x43 };
   private static bool IsTor(this ReadOnlySpan<byte> bytes)
   {
      return bytes.Slice(0, _pchOnionCat.Length).SequenceEqual(_pchOnionCat);
   }

   private static bool IsRFC4843(this ReadOnlySpan<byte> bytes)
   {
      return bytes[15 - 15] == 0x20 &&
         bytes[15 - 14] == 0x01 &&
         bytes[15 - 13] == 0x00 &&
         (bytes[15 - 12] & 0xF0) == 0x10;
   }

   private static bool IsRFC3849(this ReadOnlySpan<byte> bytes)
   {
      return bytes[15 - 15] == 0x20 &&
         bytes[15 - 14] == 0x01 &&
         bytes[15 - 13] == 0x0D &&
         bytes[15 - 12] == 0xB8;
   }

   private static readonly byte[] _anyIPv6 = IPAddress.Parse("::").GetAddressBytes();
   private static readonly byte[] _anyIPv4 = IPAddress.Parse("0.0.0.0").MapToIPv6().GetAddressBytes();
   private static readonly byte[] _iNADDR_NONE = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
   //private static readonly byte[]  = new byte[] { 0x00, 0x00, 0x00, 0x00 };
   private static bool IsValid(this ReadOnlySpan<byte> bytes, bool isIPv4)
   {
      // unspecified IPv6 address (::)
      if (bytes.SequenceEqual(_anyIPv6))
      {
         return false;
      }

      // documentation IPv6 address
      if (bytes.IsRFC3849())
      {
         return false;
      }

      if (isIPv4)
      {
         //// INADDR_NONE
         if (bytes.Slice(12, 4).SequenceEqual(_iNADDR_NONE))
         {
            return false;
         }

         //// 0
         if (bytes.Slice(12, 4).SequenceEqual(_anyIPv4))
         {
            return false;
         }
      }

      return true;
   }
}

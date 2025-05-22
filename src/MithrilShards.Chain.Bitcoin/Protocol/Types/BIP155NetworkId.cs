// Reference: https://github.com/bitcoin/bips/blob/master/bip-0155.mediawiki
namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// Network identifiers for BIP155 addrv2 message.
   /// </summary>
   public enum BIP155NetworkId : byte
   {
      /// <summary>
      /// IPv4 address (4 bytes).
      /// </summary>
      IPV4 = 1,

      /// <summary>
      /// IPv6 address (16 bytes).
      /// </summary>
      IPV6 = 2,

      /// <summary>
      /// Tor v2 onion service address (10 bytes). Deprecated.
      /// </summary>
      TORV2 = 3,

      /// <summary>
      /// Tor v3 onion service address (32 bytes).
      /// </summary>
      TORV3 = 4,

      /// <summary>
      /// I2P SAM address (32 bytes).
      /// </summary>
      I2P = 5,

      /// <summary>
      /// CJDNS address (16 bytes).
      /// </summary>
      CJDNS = 6
   }
}

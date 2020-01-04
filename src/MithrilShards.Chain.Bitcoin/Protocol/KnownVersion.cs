namespace MithrilShards.Chain.Bitcoin.Protocol
{
   /// <summary>
   /// Versions reference
   /// https://github.com/bitcoin/bitcoin/blob/master/src/version.h
   /// https://bitcoin.org/en/developer-reference#protocol-versions
   /// </summary>
   public sealed class KnownVersion
   {
      /// <summary>
      /// Version 106.
      /// Bitcoin Core 0.1.6 (Oct 2009).
      /// Added transmitter IP address fields, nonce, and User Agent (subVer) to version message.
      /// </summary>
      public const int V106 = 106;

      /// <summary>
      /// Version 209.
      /// Bitcoin Core 0.2.9 (May 2010)
      /// Added checksum field to message headers, added verack message, and added starting height field to version message.
      /// </summary>
      public const int V209 = 209;

      /// <summary>
      /// Version 311.
      /// Bitcoin Core 0.3.11 (Aug 2010).
      /// Added alert message.
      /// </summary>
      public const int V311 = 311;

      /// <summary>
      /// Version 31402.
      /// Bitcoin Core 0.3.15 (Oct 2010)
      /// Added time field to addr message.
      /// </summary>
      public const int V31402 = 31402;

      /// <summary>
      /// Version 31800.
      /// Bitcoin Core 0.3.18 (Dec 2010).
      /// Added getheaders message and headers message.
      /// </summary>
      public const int V31800 = 31800;

      /// <summary>
      /// Version 60000
      /// Bitcoin Core 0.6.0 (Mar 2012).
      /// BIP14: Separated protocol version from Bitcoin Core version.
      /// </summary>
      public const int V60000 = 60000;

      /// <summary>
      /// Version 60001.
      /// Bitcoin Core 0.6.1 (May 2012).
      /// BIP31:
      /// Added nonce field to ping message.
      /// Added pong message.
      /// </summary>
      public const int V60001 = 60001;

      /// <summary>
      /// Version 60002.
      /// Bitcoin Core 0.7.0 (Sep 2012).
      /// BIP35:
      /// Added mempool message.
      /// Extended getdata message to allow download of memory pool transactions.
      /// </summary>
      public const int V60002 = 60002;

      /// <summary>
      /// Version 70001.
      /// Bitcoin Core 0.8.0 (Feb 2013).
      ///  Added notfound message.
      ///  BIP37:
      ///  Added filterload message.
      ///  Added filteradd message.
      ///  Added filterclear message.
      ///  Added merkleblock message.
      ///  Added relay field to version message.
      ///  Added MSG_FILTERED_BLOCK inventory type to getdata message.
      /// </summary>
      public const int V70001 = 70001;

      /// <summary>
      /// Version 70002
      /// Bitcoin Core 0.9.0 (Mar 2014).
      /// Send multiple inv messages in response to a mempool message if necessary.
      /// BIP61: Added reject message
      /// </summary>
      public const int V70002 = 70002;

      /// <summary>
      /// Version 70011.
      /// Bitcoin Core 0.12.0 (Feb 2016).
      /// BIP111: filter* messages are disabled without NODE_BLOOM after and including this version.
      /// </summary>
      public const int V70011 = 70011;

      /// <summary>
      /// Version 70012.
      /// Bitcoin Core 0.12.0 (Feb 2016).
      /// BIP130: Added sendheaders message.
      /// </summary>
      public const int V70012 = 70012;


      /// <summary>
      /// Version 70013.
      /// Bitcoin Core 0.13.0 (Aug 2016).
      /// BIP133:
      /// Added feefilter message.
      /// Removed alert message system. See Alert System Retirement.
      /// </summary>
      public const int V70013 = 70013;

      /// <summary>
      /// Version 70014.
      /// Bitcoin Core 0.13.0 (Aug 2016).
      /// BIP152:
      /// Added sendcmpct, cmpctblock, getblocktxn, blocktxn messages.
      /// Added MSG_CMPCT_BLOCK inventory type to getdata message.
      /// </summary>
      public const int V70014 = 70014;

      /// <summary>
      /// Version 70015.
      /// Bitcoin Core 0.13.2 (Jan 2017).
      /// New banning behavior for invalid compact blocks #9026 in v0.14.0, Backported to v0.13.2 in #9048.
      /// </summary>
      public const int V70015 = 70015;


      public static int CurrentVersion => V70015;
   }
}

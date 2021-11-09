namespace MithrilShards.Chain.Bitcoin.Network;

/// <summary>
/// Permissions a peer may have and that will change the default behavior of the node.
/// Currently on bitcoin core the implementation is specified in this PR: https://github.com/bitcoin/bitcoin/pull/16248
/// that says that permissions can be specified by -whitelist or -whitebind argument.
/// Examples:
///   -whitelist=bloomfilter@127.0.0.1/32.
///   -whitebind=bloomfilter,relay,noban@127.0.0.1:10020.
/// </summary>
public class BitcoinPeerPermissions
{
   public const int NONE = 0;

   /// <summary>
   /// Can query bloom filter even if -peerbloomfilters is false.
   /// </summary>
   public const int BLOOMFILTER = 1 << 1;

   /// <summary>
   /// Relay and accept transactions from this peer, even if -blocksonly is true.
   /// </summary>
   public const int RELAY = 1 << 3;

   /// <summary>
   /// Always relay transactions from this peer, even if already in mempool.
   /// Keep parameter interaction: forcerelay implies relay.
   /// </summary>
   public const int FORCERELAY = (1 << 2) | RELAY;

   /// <summary>
   /// Allow getheaders during IBD and block-download after maxuploadtarget limit.
   /// </summary>
   public const int DOWNLOAD = 1 << 6;

   /// <summary>
   /// Can't be banned for misbehavior.
   /// </summary>
   public const int NOBAN = (1 << 4) | DOWNLOAD;

   /// <summary>
   /// Can query the mempool.
   /// </summary>
   public const int MEMPOOL = 1 << 5;

   /// <summary>
   /// Can request addrs without hitting a privacy-preserving cache.
   /// </summary>
   public const int ADDR = 1 << 7;

   /// <summary>
   /// True if the user did not specifically set fine grained permissions.
   /// </summary>
   public const int ISIMPLICIT = 1 << 31;

   public const int ALL = BLOOMFILTER | FORCERELAY | RELAY | NOBAN | MEMPOOL | DOWNLOAD | ADDR;

   private int _permissions = NONE;
   public int Permissions => _permissions;

   public void Add(int permission)
   {
      _permissions |= permission;
   }

   public void Remove(int permission)
   {
      _permissions &= ~permission;
   }

   public bool Has(int permission)
   {
      return (_permissions & permission) == permission;
   }

   public void Reset()
   {
      _permissions = NONE;
   }
}

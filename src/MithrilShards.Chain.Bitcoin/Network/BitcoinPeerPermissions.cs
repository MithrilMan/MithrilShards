namespace MithrilShards.Chain.Bitcoin.Network
{
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
      /// Always relay transactions from this peer, even if already in mempool
      /// Keep parameter interaction: forcerelay implies relay
      /// </summary>
      public const int FORCERELAY = 1 << 2 | RELAY;

      /// <summary>
      /// Can't be banned for misbehavior
      /// </summary>
      public const int NOBAN = 1 << 4;

      /// <summary>
      /// Can query the mempool
      /// </summary>
      public const int MEMPOOL = 1 << 5;

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
}

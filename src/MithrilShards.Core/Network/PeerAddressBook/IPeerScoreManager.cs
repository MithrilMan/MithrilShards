namespace MithrilShards.Core.Network.PeerAddressBook
{
   /// <summary>
   /// Provides an interface for implementing an address book needed to connect to other peers.
   /// </summary>
   public interface IPeerScoreManager
   {

      /// <summary>
      /// Adds the score to a peer.
      /// </summary>
      /// <param name="peer">The peer to which increase the score amount.</param>
      /// <param name="amount">The amount to add to the peer total score.</param>
      /// <returns></returns>
      uint IncreaseScore(IPeerContext peer, int amount);

      /// <summary>
      /// Adds the score to a peer.
      /// </summary>
      /// <param name="peer">The peer to which decrease the score amount.</param>
      /// <param name="amount">The amount to remove from the peer total score.</param>
      /// <returns></returns>
      uint DecreaseScore(IPeerContext peer, int amount);
   }
}

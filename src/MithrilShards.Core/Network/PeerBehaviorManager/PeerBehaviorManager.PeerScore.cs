namespace MithrilShards.Core.Network.PeerBehaviorManager
{
   public partial class DefaultPeerBehaviorManager
   {
      private class PeerScore
      {
         public IPeerContext PeerContext { get; }
         public int Score { get; private set; }

         public PeerScore(IPeerContext peerContext, int initialScore)
         {
            this.PeerContext = peerContext;
            this.Score = initialScore;
         }

         public int UpdateScore(int amount)
         {
            this.Score += amount;
            return this.Score;
         }
      }
   }
}

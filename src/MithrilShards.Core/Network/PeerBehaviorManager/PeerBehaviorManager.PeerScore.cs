namespace MithrilShards.Core.Network.PeerBehaviorManager;

public partial class DefaultPeerBehaviorManager
{
   private class PeerScore
   {
      public IPeerContext PeerContext { get; }
      public int Score { get; private set; }

      public PeerScore(IPeerContext peerContext, int initialScore)
      {
         PeerContext = peerContext;
         Score = initialScore;
      }

      public int UpdateScore(int amount)
      {
         Score += amount;
         return Score;
      }
   }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MithrilShards.Core.Statistics;

namespace MithrilShards.Core.Network.PeerBehaviorManager
{
   public partial class PeerBehaviorManager : IStatisticFeedsProvider
   {
      private const string FEED_PEERS_SCORE = "ConnectedPeers";
      private const int STATISTIC_REFRESH_RATE = 15;
      private readonly IStatisticFeedsCollector statisticFeedsCollector;

      public List<string[]> GetStatisticFeedValues(string feedId)
      {
         return feedId switch
         {
            FEED_PEERS_SCORE => (
               from peerScore in this.connectedPeers.Values.ToList()
               orderby peerScore.Score descending
               select new string[] {
                  peerScore.PeerContext.PeerId,
                  peerScore.PeerContext.RemoteEndPoint.ToString(),
                  peerScore.Score.ToString(),
               }).ToList(),
            _ => null
         };
      }

      public void RegisterStatisticFeeds()
      {
         this.statisticFeedsCollector.RegisterStatisticFeeds(this,
            new StatisticFeedDefinition(
               FEED_PEERS_SCORE,
               "Peers Score",
               new List<FieldDefinition>{
                  new FieldDefinition("Peer Id", "Peer unique identifier.", 38),
                  new FieldDefinition("EndPoint","Peer End Point.", 30),
                  new FieldDefinition("Score","Peer Score.", 15)
               },
               TimeSpan.FromSeconds(STATISTIC_REFRESH_RATE)
            )
         );
      }
   }
}

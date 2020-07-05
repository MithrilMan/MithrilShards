using Microsoft.Extensions.Hosting;

namespace MithrilShards.Core.Network.PeerBehaviorManager
{
   public interface IPeerBehaviorManager : IHostedService
   {
      /// <summary>
      /// Inflicts a negative score to the peer because of a misbehavior.
      /// The peer may be banned if his score is lower than a specific threshold.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      /// <param name="penalty">The penalty.</param>
      /// <param name="reason">The reason.</param>
      void Misbehave(IPeerContext peerContext, uint penalty, string reason);

      /// <summary>
      /// Adds a bonus to the peer score for having done a good action.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      /// <param name="bonus">The bonus.</param>
      /// <param name="reason">The reason.</param>
      void AddBonus(IPeerContext peerContext, uint bonus, string reason);

      /// <summary>
      /// Gets the peer score.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      /// <returns></returns>
      int GetScore(IPeerContext peerContext);
   }
}
using MithrilShards.Chain.Bitcoin.Protocol.Messages;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors;

public partial class PingPongProcessor
{
   private readonly Status _status = new();

   public class Status
   {
      /// <summary>
      /// Gets or sets the last ping request time (usec).
      /// </summary>
      public long PingRequestTime { get; private set; } = 0;

      /// <summary>
      /// The pong nonce reply we're expecting, or 0 if no pong expected.
      /// </summary>
      public ulong PingRequestNonce { get; private set; } = 0;

      /// <summary>
      /// Last measured round-trip time.
      /// </summary>
      public long PingResponseTime { get; private set; } = 0;


      internal void PingSent(long pingRequestTime, PingMessage ping)
      {
         PingRequestTime = pingRequestTime;
         PingRequestNonce = ping.Nonce;
         PingResponseTime = 0;
      }

      internal (ulong Nonce, long RoundTrip) PongReceived(long responseTime)
      {
         (ulong Nonce, long RoundTrip) result = (PingRequestNonce, responseTime - PingRequestTime);

         PingResponseTime = responseTime;
         PingRequestTime = 0;
         PingRequestNonce = 0;

         return result;
      }
   }
}

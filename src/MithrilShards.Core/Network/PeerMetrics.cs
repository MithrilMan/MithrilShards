namespace MithrilShards.Core.Network
{
   public class PeerMetrics
   {
      private long receivedBytes;
      private long sentBytes;
      private long wastedBytes;

      /// <summary>
      /// Gets or sets bytes received from this peer.
      /// </summary>
      /// <value>
      /// The bytes received by the peer.
      /// </value>
      public long ReceivedBytes => this.receivedBytes;
      /// <summary>
      /// Gets or sets bytes sent to this peer.
      /// </summary>
      /// <value>
      /// The bytes sent to the peer.
      /// </value>
      public long SentBytes => this.sentBytes;
      /// <summary>
      /// Gets or sets bytes received but skipped because wasn't part of the protocol.
      /// </summary>
      /// <value>
      /// The bytes received by the peer but skipped because not useful.
      /// </value>
      public long WastedBytes => this.wastedBytes;

      public void Received(long amount)
      {
         this.receivedBytes += amount;
      }
      public void Sent(long amount)
      {
         this.sentBytes += amount;
      }

      public void Wasted(long amount)
      {
         this.wastedBytes += amount;
      }
   }
}

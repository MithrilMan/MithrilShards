using System;

namespace MithrilShards.Core.Network;

public class PeerMetrics
{
   /// <summary>
   /// Gets or sets bytes received from this peer.
   /// </summary>
   /// <value>
   /// The bytes received by the peer.
   /// </value>
   public long ReceivedBytes { get; private set; }

   /// <summary>
   /// Gets or sets bytes sent to this peer.
   /// </summary>
   /// <value>
   /// The bytes sent to the peer.
   /// </value>
   public long SentBytes { get; private set; }

   /// <summary>
   /// Gets or sets bytes received but skipped because wasn't part of the protocol.
   /// </summary>
   /// <value>
   /// The bytes received by the peer but skipped because not useful.
   /// </value>
   public long WastedBytes { get; private set; }

   /// <summary>
   /// Gets the last activity (last time data has been sent or received).
   /// </summary>
   /// <value>
   /// The last activity.
   /// </value>
   public DateTimeOffset LastActivity { get; private set; }

   public void Received(long amount)
   {
      ReceivedBytes += amount;
      LastActivity = DateTimeOffset.UtcNow;
   }
   public void Sent(long amount)
   {
      SentBytes += amount;
      LastActivity = DateTimeOffset.UtcNow;
   }

   /// <summary>
   /// Flag as wasted the specified amount of data, and consider the same amount as received data too.
   /// </summary>
   /// <param name="amount">The amount.</param>
   public void Wasted(long amount)
   {
      ReceivedBytes += amount;
      WastedBytes += amount;
   }
}

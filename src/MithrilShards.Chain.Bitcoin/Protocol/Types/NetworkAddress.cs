using System;

namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// Network address (net_addr).
   /// </summary>
   public class NetworkAddress : NetworkAddressNoTime
   {
      /// <summary>
      /// The Time (version >= 31402). Not present in version message (use <see cref="NetworkAddressNoTime"/> in Version message).
      /// </summary>
      public DateTimeOffset Time { get; set; }
   }
}

using System;
using System.Threading.Tasks;

namespace MithrilShards.Core.Network.Protocol.Processors
{
   /// <summary>
   /// Interfaces that define a generic network message
   /// </summary>
   public interface INetworkMessageProcessor : IDisposable
   {
      bool Enabled { get; }

      ValueTask AttachAsync(IPeerContext peerContext);
   }
}

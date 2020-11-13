using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Network
{
   public abstract class BitcoinNetworkMessageSerializerBase<TMessageType> : NetworkMessageSerializerBase<TMessageType, BitcoinPeerContext>
      where TMessageType : INetworkMessage, new()
   {
   }
}
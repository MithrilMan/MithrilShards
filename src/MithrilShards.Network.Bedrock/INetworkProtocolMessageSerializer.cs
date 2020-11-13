using Bedrock.Framework.Protocols;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Network.Bedrock
{
   public interface INetworkProtocolMessageSerializer : IMessageReader<INetworkMessage>, IMessageWriter<INetworkMessage>
   {
      /// <summary>
      /// Inject into the peer to the network message protocol instance.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      void SetPeerContext(IPeerContext peerContext);
   }
}
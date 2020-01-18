using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   [NetworkMessage(COMMAND)]
   public sealed class GetAddrMessage : INetworkMessage
   {
      private const string COMMAND = "getaddr";
      string INetworkMessage.Command => COMMAND;
   }
}
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages
{
   public class NetworkMessage : INetworkMessage
   {
      public string Command { get; }


      public NetworkMessage(string command)
      {
         this.Command = command;
      }
   }
}
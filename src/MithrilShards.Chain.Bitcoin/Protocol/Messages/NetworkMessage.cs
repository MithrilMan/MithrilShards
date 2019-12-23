using MithrilShards.Core.Network.Protocol;
using System.Text;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages {
   public class NetworkMessage : INetworkMessage {
      public string Command { get; }


      public NetworkMessage(string command) {
         this.Command = command;
      }
   }
}
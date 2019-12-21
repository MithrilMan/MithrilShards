using MithrilShards.Core.Network.Protocol;
using System.Text;

namespace MithrilShards.Chain.Bitcoin.Protocol {
   public class NetworkMessage : INetworkMessage {
      public uint Magic { get; set; }

      byte[] command = new byte[12];
      public string Command {
         get {
            return Encoding.ASCII.GetString(this.command);
         }
         private set {
            command = Encoding.ASCII.GetBytes(value.Trim().PadRight(12, '\0'));
         }
      }

      public byte[] Payload { get; private set; }
   }
}
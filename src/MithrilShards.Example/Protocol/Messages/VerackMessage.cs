using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Example.Protocol.Messages
{
   /// <summary>
   /// A version acknowledge message.
   /// </summary>
   /// <seealso cref="INetworkMessage" />
   [NetworkMessage(COMMAND)]
   public sealed class VerackMessage : INetworkMessage
   {
      private const string COMMAND = "verack";
      string INetworkMessage.Command => COMMAND;
   }
}
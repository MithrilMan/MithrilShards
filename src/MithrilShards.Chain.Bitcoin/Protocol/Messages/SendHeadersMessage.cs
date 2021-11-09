using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages;

/// <summary>
/// Request for Direct headers announcement.
/// Upon receipt of this message, the node is be permitted, but not required, to announce new blocks
/// by headers command(instead of inv command).
/// This message is supported by the protocol version >= 70012 or Bitcoin Core version >= 0.12.0.
/// See BIP 130 for more information.
/// </summary>
/// <seealso cref="INetworkMessage" />
[NetworkMessage(COMMAND)]
public sealed class SendHeadersMessage : INetworkMessage
{
   private const string COMMAND = "sendheaders";
   string INetworkMessage.Command => COMMAND;
}

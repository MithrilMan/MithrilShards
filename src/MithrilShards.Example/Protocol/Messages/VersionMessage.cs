using System;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;


namespace MithrilShards.Example.Protocol.Messages;

/// <summary>
/// A simplified version of a bitcoin Version message
/// </summary>
/// <seealso cref="INetworkMessage" />
[NetworkMessage(COMMAND)]
public sealed class VersionMessage : INetworkMessage
{
   private const string COMMAND = "version";
   string INetworkMessage.Command => COMMAND;

   /// <summary>
   /// Identifies protocol version being used by the node
   /// </summary>
   public int Version { get; set; }

   /// <summary>
   /// standard UNIX timestamp in seconds
   /// </summary>
   public DateTimeOffset Timestamp { get; set; }

   /// <summary>
   /// Node random nonce, randomly generated every time a version packet is sent. This nonce is used to detect connections to self.
   /// </summary>
   public ulong Nonce { get; set; }

   /// <summary>
   /// User Agent (0x00 if string is 0 bytes long)
   /// </summary>
   public string? UserAgent { get; set; }
}

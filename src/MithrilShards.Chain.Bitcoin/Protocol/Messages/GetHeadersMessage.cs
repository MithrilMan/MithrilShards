using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages;

/// <summary>
/// Requires a headers packet containing the headers of blocks starting right after the last known hash in the block
/// locator object, up to hash_stop or 2000 blocks, whichever comes first. To receive the next block headers, one
/// needs to issue getheaders again with a new block locator object.
/// Keep in mind that some clients may provide headers of blocks which are invalid if the block locator object contains a hash on the invalid branch.
/// </summary>
/// <seealso cref="INetworkMessage" />
[NetworkMessage(COMMAND)]
public sealed class GetHeadersMessage : INetworkMessage
{
   private const string COMMAND = "getheaders";
   string INetworkMessage.Command => COMMAND;

   /// <summary>
   /// The protocol version.
   /// </summary>
   public uint Version { get; set; }

   /// <summary>
   /// Block locator objects.
   /// Newest back to genesis block (dense to start, but then sparse)
   /// </summary>
   public BlockLocator? BlockLocator { get; set; }

   /// <summary>
   /// Hash of the last desired block header
   /// Set to zero to get as many blocks as possible (2000 by default)
   /// </summary>
   public UInt256? HashStop { get; set; }
}

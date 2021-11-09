using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Messages;

/// <summary>
/// Requests an inv packet containing the list of blocks starting right after the last known hash in the block locator object,
/// up to hash_stop or 500 blocks, whichever comes first.
/// The locator hashes are processed by a node in the order as they appear in the message.
/// If a block hash is found in the node's main chain, the list of its children is returned back via the inv message and the
/// remaining locators are ignored, no matter if the requested limit was reached, or not.
/// To receive the next blocks hashes, one needs to issue getblocks again with a new block locator object.
/// Keep in mind that some clients may provide blocks which are invalid if the block locator object contains a hash on the invalid branch.
/// </summary>
/// <seealso cref="INetworkMessage" />
[NetworkMessage(COMMAND)]
public sealed class GetBlocksMessage : INetworkMessage
{
   private const string COMMAND = "getblocks";
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
   /// Hash of the last desired block.
   /// Set to zero to get as many blocks as possible (500 by default)
   /// </summary>
   public UInt256? HashStop { get; set; }
}

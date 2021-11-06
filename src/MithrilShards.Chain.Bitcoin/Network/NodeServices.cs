using System;

namespace MithrilShards.Chain.Bitcoin.Network
{
   /// <summary>
   /// From https://github.com/bitcoin/bitcoin/blob/1dbf3350c683f93d7fc9b861400724f6fd2b2f1d/src/protocol.h#L262-L268
   /// Bits 24-31 are reserved for temporary experiments. Just pick a bit that
   /// isn't getting used, or one not being used much, and notify the
   /// bitcoin-development mailing list. Remember that service bits are just
   /// unauthenticated advertisements, so your code must be robust against
   /// collisions and other cases where nodes may be advertising a service they
   /// do not actually support. Other service bits should be allocated via the
   /// BIP process.
   /// </summary>
   [Flags]
   public enum NodeServices : uint
   {
      /// <summary>
      /// Nothing.
      /// </summary>
      None = 0,

      /// <summary>
      /// NODE_NETWORK means that the node is capable of serving the block chain. It is currently
      /// set by all Bitcoin Core nodes, and is unset by SPV clients or other peers that just want
      /// network services but don't provide them.
      /// </summary>
      Network = 1 << 0,

      /// <summary>
      ///  NODE_GETUTXO means the node is capable of responding to the getutxo protocol request.
      /// Bitcoin Core does not support this but a patch set called Bitcoin XT does.
      /// See BIP 64 for details on how this is implemented.
      /// </summary>
      GetUTXO = 1 << 1,

      /// <summary> NODE_BLOOM means the node is capable and willing to handle bloom-filtered connections.
      /// Bitcoin Core nodes used to support this by default, without advertising this bit,
      /// but no longer do as of protocol version 70011 (= NO_BLOOM_VERSION)
      /// </summary>
      Bloom = 1 << 2,

      /// <summary> Indicates that a node can be asked for blocks and transactions including witness data.</summary>
      Witness = 1 << 3,

      /// <summary>
      /// NODE_NETWORK_LIMITED means the same as NODE_NETWORK with the limitation of only
      /// serving the last 288 (2 day) blocks
      /// See BIP159 for details on how this is implemented.
      /// </summary>
      NetworkLimited = 1 << 10,
   }
}

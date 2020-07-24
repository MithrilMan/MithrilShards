using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Validator
{
   /// <summary>
   /// Represents a block that has to be validated.
   /// </summary>
   public class BlockToValidate
   {
      /// <summary>
      /// The block that has to be validated.
      /// </summary>
      public Protocol.Types.Block Block { get; }

      /// <summary>
      /// The peer that's requiring the validation.
      /// Null if the request comes from other sources (e.g. validating a block fetched from disk)
      /// </summary>
      public IPeerContext? Peer { get; }

      public BlockToValidate(Protocol.Types.Block block, IPeerContext? peer)
      {
         this.Block = block;
         this.Peer = peer;
      }
   }
}

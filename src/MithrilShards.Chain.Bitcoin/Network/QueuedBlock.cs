using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Network
{
   /// <summary>
   /// Represents a block queued to be downloaded.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Network.IBlockDownloader" />
   public class QueuedBlock
   {
      public UInt256 BlockHash { get; }

      /// <summary>
      /// Gets the header node.
      /// </summary>
      /// <value>
      /// The header node.
      /// </value>
      public HeaderNode HeaderNode { get; }

      /// <summary>
      /// Gets a value indicating whether this block has validated headers at the time of request.
      /// </summary>
      /// <value>
      ///   <c>true</c> if the block header is already validated; otherwise, <c>false</c>.
      /// </value>
      public bool ValidatedHeaders { get; }

      public QueuedBlock(HeaderNode headerNode /*TODO: ,PartiallyDownloadedBlock partialBlock*/)
      {
         this.BlockHash = headerNode.Hash;
         this.HeaderNode = headerNode;
         this.ValidatedHeaders = headerNode != null;
      }
   }
}

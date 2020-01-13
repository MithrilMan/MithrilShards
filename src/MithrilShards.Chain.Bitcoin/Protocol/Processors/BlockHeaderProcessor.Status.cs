namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class BlockHeaderProcessor
   {
      private readonly Status status = new Status();

      private class Status
      {
         public int PeerStartingHeight { get; internal set; } = 0;

         /// <summary>
         /// Gets or sets a value indicating whether the peer prefer to use compact block mode.
         /// See BIP 152 for details.
         /// </summary>
         public bool UseCompactBlocks { get; internal set; } = false;

         /// <summary>
         /// Holds the reference of the version to use to send compact messages.
         /// See BIP 152 for details.
         /// </summary>
         /// <value>
         ///   <c>true</c> if [use compact version]; otherwise, <c>false</c>.
         /// </value>
         public ulong CompactVersion { get; set; } = 0;

         /// <summary>
         /// When true, enable compact messaging using high bandwidth mode.
         /// See BIP 152 for details.
         /// </summary>
         public bool CompactBlocksHighBandwidthMode { get; set; } = false;

         /// <summary>
         /// Gets or sets a value indicating whether new block should be announced using send headers, see BIP 130.
         /// </summary>
         public bool AnnounceNewBlockUsingSendHeaders { get; internal set; } = false;
      }
   }
}
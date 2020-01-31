using System;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class BlockHeaderProcessor
   {
      private readonly BlockHeaderProcessorStatus status = new BlockHeaderProcessorStatus();

      public class BlockHeaderProcessorStatus
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
         public ulong CompactVersion { get; internal set; } = 0;

         /// <summary>
         /// When true, enable compact messaging using high bandwidth mode.
         /// See BIP 152 for details.
         /// </summary>
         public bool CompactBlocksHighBandwidthMode { get; internal set; } = false;

         /// <summary>
         /// Gets or sets a value indicating whether new block should be announced using send headers, see BIP 130.
         /// </summary>
         public bool AnnounceNewBlockUsingSendHeaders { get; internal set; } = false;

         /// <summary>
         /// The unconnecting headers counter, used to issue a misbehavior penalty when exceed the expected threshold.
         /// It gets reset to 0 when a header connects successfully.
         /// </summary>
         public int UnconnectingHeaderReceived { get; internal set; } = 0;

         /// <summary>
         /// Gets or sets the last unknown block hash.
         /// </summary>
         /// <value>
         /// The last unknown block hash.
         /// </value>
         public UInt256? LastUnknownBlockHash { get; internal set; } = null;

         /// <summary>
         /// Gets or sets the best known block we know this peer has announced.
         /// </summary>
         /// <value>
         /// The best known header.
         /// </value>
         public HeaderNode? BestKnownHeader { get; internal set; }

         /// <summary>
         /// Gets the time of last new block announcement.
         /// </summary>
         /// <value>
         /// The last block announcement time (epoch).
         /// </value>
         public long LastBlockAnnouncement { get; internal set; }
      }
   }
}
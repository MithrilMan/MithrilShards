using System;
using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class BlockHeaderProcessor
   {
      private readonly BlockHeaderProcessorStatus status = new BlockHeaderProcessorStatus();

      public class BlockHeaderProcessorStatus
      {
         /// <summary>
         /// Whether the peer is a limited node (isn't a full node and has only a limited amount of blocks to serve).
         /// (obtained from Version message).
         /// </summary>
         public bool IsLimitedNode { get; internal set; } = false;

         /// <summary>
         /// Whether this peer is a client.
         /// A Client is a node not relaying blocks and tx and not serving (parts) of the historical blockchain as "clients".
         /// (obtained from Version message).
         /// </summary>
         public bool IsClient { get; internal set; } = false;

         public int PeerStartingHeight { get; internal set; } = 0;

         /// <summary>
         /// If we've announced NODE_WITNESS to this peer: whether the peer sends witnesses in cmpctblocks/blocktxns,
         /// otherwise: whether this peer sends non-witnesses in cmpctblocks/blocktxns.
         /// </summary>
         public bool SupportsDesiredCompactVersion { get; internal set; } = false;

         /// <summary>
         /// Whether this peer will send us cmpctblocks if we request them (fProvidesHeaderAndIDs).
         /// This is not used to gate request logic, as we really only care about fSupportsDesiredCmpctVersion,
         /// but is used as a flag to "lock in" the version of compact blocks(fWantsCmpctWitness) we send.
         /// </summary>
         public bool ProvidesHeaderAndIDs { get; set; } = false;

         /// <summary>
         /// When true, enable compact messaging using high bandwidth mode.
         /// See BIP 152 for details.
         /// </summary>
         public bool AnnounceUsingCompactBlock { get; internal set; } = false;

         /// <summary>
         /// Whether new block should be announced using send headers, see BIP 130.
         /// </summary>
         public bool AnnounceNewBlockUsingSendHeaders { get; internal set; } = false;

         /// <summary>
         /// The unconnecting headers counter, used to issue a misbehavior penalty when exceed the expected threshold.
         /// It gets reset to 0 when a header connects successfully.
         /// </summary>
         public int UnconnectingHeaderReceived { get; internal set; } = 0;

         /// <summary>
         /// The hash of the last unknown block this peer has announced.
         /// </summary>
         public UInt256? LastUnknownBlockHash { get; internal set; } = null;

         /// <summary>
         /// Gets or sets the best known block we know this peer has announced.
         /// </summary>
         public HeaderNode? BestKnownHeader { get; internal set; } = null;

         /// <summary>
         /// The best header we have sent to the peer.
         /// </summary>
         public HeaderNode? BestSentHeader { get; internal set; } = null;

         /// <summary>
         /// Gets the time of last new block announcement.
         /// </summary>
         public long LastBlockAnnouncement { get; internal set; } = 0;

         /// <summary>
         /// Whether this peer can give us witnesses. (fHaveWitness)
         /// </summary>
         public bool CanServeWitness { get; internal set; } = false;

         /// <summary>
         /// Gets or sets a value indicating whether this peer wants witnesses in cmpctblocks/blocktxns.
         /// </summary>
         public bool WantsCompactWitness { get; internal set; } = false;

         /// <summary>
         /// Gets a value indicating whether this peer is synchronizing headers with our node.
         /// </summary>
         public bool IsSynchronizingHeaders { get; internal set; } = false;

         /// <summary>
         /// When to potentially disconnect the peer for stalling headers download.
         /// </summary>
         public long HeadersSyncTimeout { get; internal set; } = long.MaxValue;
      }
   }
}
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors;

public partial class SynchronizationProcessor
{
   private readonly BlockHeaderProcessorStatus _status = new();

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
      /// Gets a value indicating whether this peer is synchronizing headers with our node.
      /// </summary>
      public bool IsSynchronizingHeaders { get; internal set; } = false;

      /// <summary>
      /// When to potentially disconnect the peer for stalling headers download.
      /// </summary>
      public long HeadersSyncTimeout { get; internal set; } = long.MaxValue;

      /// <summary>
      /// Gets the last accepted block time.
      /// </summary>
      public long LastBlockTime { get; internal set; } = 0;
   }
}

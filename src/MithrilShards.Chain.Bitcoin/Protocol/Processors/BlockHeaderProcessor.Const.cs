using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class BlockHeaderProcessor
   {
      /// <summary>
      /// Number of headers sent in one getheaders result.
      /// We rely on the assumption that if a peer sends less than this number, we reached its tip.
      /// Changing this value is a protocol upgrade.
      /// </summary>
      private const int MAX_HEADERS = 2000;

      /// <summary>
      /// Maximum number of headers to announce when relaying blocks with headers message.
      /// </summary>
      private const int MAX_BLOCKS_TO_ANNOUNCE = 8;

      /// <summary>
      /// Maximum number of unconnecting headers before triggering a peer Misbehave action.
      /// </summary>
      private const int MAX_UNCONNECTING_HEADERS = 10;

      /// <summary>
      /// The maximum number of blocks that can be requested from a single peer.
      /// </summary>
      private const int MAX_BLOCKS_IN_TRANSIT_PER_PEER = 26; //FIX default bitcoin value: 16

      /// <summary>
      /// Maximum number of block hashes allowed in the BlockLocator.</summary>
      /// <seealso cref="https://lists.linuxfoundation.org/pipermail/bitcoin-dev/2018-August/016285.html"/>
      /// <seealso cref="https://github.com/bitcoin/bitcoin/pull/13907"
      /// </summary>
      private const int MAX_LOCATOR_SIZE = 101;

      /// <summary>
      /// Base Headers download timeout expressed in microseconds (usec)
      /// final Timeout = <see cref="HEADERS_DOWNLOAD_TIMEOUT_BASE"/> + <see cref="HEADERS_DOWNLOAD_TIMEOUT_PER_HEADER"/> * (expected number of headers)
      /// </summary>
      private const int HEADERS_DOWNLOAD_TIMEOUT_BASE = 15 * 60 * 1_000_000; // 15 minutes;

      /// <summary>
      /// Per-Header download timeout expressed in microseconds (usec)
      /// final Timeout = <see cref="HEADERS_DOWNLOAD_TIMEOUT_BASE"/> + <see cref="HEADERS_DOWNLOAD_TIMEOUT_PER_HEADER"/> * (expected number of headers)
      /// </summary>
      private const int HEADERS_DOWNLOAD_TIMEOUT_PER_HEADER = 1000; // 1ms/header

      /// <summary>
      /// The header synchronization loop interval
      /// </summary>
      private const int SYNC_LOOP_INTERVAL = 250;

      /// <summary>
      /// The block request loop interval
      /// </summary>
      private const int BLOCK_REQUEST_INTERVAL = 250;

      /// <summary>
      /// Size of the "block download window": how far ahead of our current height do we fetch?
      /// Larger windows tolerate larger download speed differences between peer, but increase the potential
      /// degree of disordering of blocks on disk(which make re-indexing and pruning harder).
      /// We'll probably want to make this a per-peer adaptive value at some point.
      /// </summary>
      private const int BLOCK_DOWNLOAD_WINDOW = 1024;
   }
}

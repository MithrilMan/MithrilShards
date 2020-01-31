using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Network
{
   public interface IBlockDownloader
   {
      int BlocksInDownload { get; }

      bool TryDownloadBlock(PeerContext peerContext, HeaderNode blockToDownload, [MaybeNullWhen(false)] out QueuedBlock queuedBlock));
   }
}
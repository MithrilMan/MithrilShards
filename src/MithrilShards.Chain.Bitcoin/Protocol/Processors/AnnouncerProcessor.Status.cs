using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors;

public partial class AnnouncerProcessor
{
   private readonly AnnouncerProcessorStatus _status = new();

   public class AnnouncerProcessorStatus
   {
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
      /// Gets or sets a value indicating whether this peer wants witnesses in cmpctblocks/blocktxns.
      /// </summary>
      public bool WantsCompactWitness { get; internal set; } = false;

      /// <summary>
      /// Whether this peer will send us cmpctblocks if we request them (fProvidesHeaderAndIDs).
      /// This is not used to gate request logic, as we really only care about fSupportsDesiredCmpctVersion,
      /// but is used as a flag to "lock in" the version of compact blocks(fWantsCmpctWitness) we send.
      /// </summary>
      public bool ProvidesHeaderAndIDs { get; set; } = false;

      /// <summary>
      /// If we've announced NODE_WITNESS to this peer: whether the peer sends witnesses in cmpctblocks/blocktxns,
      /// otherwise: whether this peer sends non-witnesses in cmpctblocks/blocktxns.
      /// </summary>
      public bool SupportsDesiredCompactVersion { get; internal set; } = false;
   }
}

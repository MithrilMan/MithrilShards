using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerAddressBook;

namespace MithrilShards.Example.Network.Server.Guards
{
   public class BannedPeerGuard : ServerPeerConnectionGuardBase
   {
      readonly IPeerAddressBook peerAddressBook;

      public BannedPeerGuard(ILogger<BannedPeerGuard> logger, IOptions<ForgeConnectivitySettings> settings, IPeerAddressBook peerAddressBook) : base(logger, settings)
      {
         this.peerAddressBook = peerAddressBook;
      }

      internal override string? TryGetDenyReason(IPeerContext peerContext)
      {
         if (this.peerAddressBook.IsBanned(peerContext))
         {
            return "Inbound connection refused: peer is banned.";
         }

         return null;
      }
   }
}
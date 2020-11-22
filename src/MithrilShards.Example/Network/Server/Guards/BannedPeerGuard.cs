using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerAddressBook;

namespace MithrilShards.Example.Network.Server.Guards
{
   public class BannedPeerGuard : ServerPeerConnectionGuardBase
   {
      readonly IPeerAddressBook _peerAddressBook;

      public BannedPeerGuard(ILogger<BannedPeerGuard> logger, IOptions<ForgeConnectivitySettings> settings, IPeerAddressBook peerAddressBook) : base(logger, settings)
      {
         _peerAddressBook = peerAddressBook;
      }

      internal override string? TryGetDenyReason(IPeerContext peerContext)
      {
         if (_peerAddressBook.IsBanned(peerContext))
         {
            return "Inbound connection refused: peer is banned.";
         }

         return null;
      }
   }
}
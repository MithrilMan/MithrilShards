using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Network.PeerAddressBook
{
   public class DefaultPeerAddressBook : IPeerAddressBook
   {
      private readonly ILogger<DefaultPeerAddressBook> logger;


      public DefaultPeerAddressBook(ILogger<DefaultPeerAddressBook> logger)
      {
         this.logger = logger;
      }

      /// <inheritdoc/>
      public void AddAddress(IPeerContext peer)
      {
         this.LogNotImplementedWarning();
      }

      /// <inheritdoc/>
      public void AddAddresses(IEnumerable<IPeerContext> peers)
      {
         this.LogNotImplementedWarning();
      }

      /// <inheritdoc/>
      public void Ban(IPeerContext peer, DateTimeOffset until, string reason)
      {
         this.logger.LogDebug("Banning peer {RemoteEndPoint}: {BanReason}", peer.RemoteEndPoint, reason);
         this.LogNotImplementedWarning();
      }


      /// <inheritdoc/>
      public void RemoveAddress(IPeerContext peer)
      {
         this.LogNotImplementedWarning();
      }

      /// <inheritdoc/>
      public void RemoveAddresses(IEnumerable<IPeerContext> peers)
      {
         this.LogNotImplementedWarning();
      }

      /// <inheritdoc/>
      public bool IsBanned(IPeerContext peer)
      {
         this.LogNotImplementedWarning();

         return false;
      }

      private void LogNotImplementedWarning([CallerMemberName] string methodName = null!)
      {
         this.logger.LogWarning($"{methodName} not implemented ");
      }
   }
}

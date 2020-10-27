using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Network.PeerAddressBook
{
   public class DefaultPeerAddressBook : IPeerAddressBook
   {
      readonly ILogger<DefaultPeerAddressBook> logger;

      public DefaultPeerAddressBook(ILogger<DefaultPeerAddressBook> logger)
      {
         this.logger = logger;
      }

      public void AddAddress(IPeerContext peer)
      {
         throw new NotImplementedException();
      }

      public void AddAddresses(IEnumerable<IPeerContext> peers)
      {
         throw new NotImplementedException();
      }

      public void Ban(IPeerContext peer, DateTimeOffset until, string reason)
      {
         this.logger.LogDebug("Banning peer {RemoteEndPoint}: {BanReason}", peer.RemoteEndPoint, reason);
         throw new NotImplementedException();
      }

      public void RemoveAddress(IPeerContext peer)
      {
         throw new NotImplementedException();
      }

      public void RemoveAddresses(IEnumerable<IPeerContext> peers)
      {
         throw new NotImplementedException();
      }
   }
}

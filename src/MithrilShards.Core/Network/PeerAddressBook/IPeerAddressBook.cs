using System;
using System.Collections.Generic;

namespace MithrilShards.Core.Network.PeerAddressBook
{
   /// <summary>
   /// Provides an interface for implementing an address book needed to connect to other peers.
   /// </summary>
   public interface IPeerAddressBook : IPeerScoreManager
   {
      void AddAddress(IPeerContext peer);
      void AddAddresses(IEnumerable<IPeerContext> peers);

      void RemoveAddress(IPeerContext peer);
      void RemoveAddresses(IEnumerable<IPeerContext> peers);

      void Ban(IPeerContext peer, DateTimeOffset until, string reason);
   }
}

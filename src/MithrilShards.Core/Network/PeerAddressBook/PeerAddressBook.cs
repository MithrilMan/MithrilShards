using System;
using System.Collections.Generic;

namespace MithrilShards.Core.Network.PeerAddressBook
{
   public class PeerAddressBook : IPeerAddressBook, IPeerScoreManager
   {
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
         throw new NotImplementedException();
      }

      public uint DecreaseScore(IPeerContext peer, int amount)
      {
         throw new NotImplementedException();
      }

      public uint IncreaseScore(IPeerContext peer, int amount)
      {
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

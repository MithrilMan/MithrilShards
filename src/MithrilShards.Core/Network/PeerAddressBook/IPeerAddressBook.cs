using System;
using System.Collections.Generic;

namespace MithrilShards.Core.Network.PeerAddressBook;

/// <summary>
/// Provides an interface for implementing an address book needed to connect to other peers.
/// </summary>
public interface IPeerAddressBook
{
   /// <summary>
   /// Adds the address of the peer to the address book.
   /// </summary>
   /// <param name="peer">The peer to add.</param>
   void AddAddress(IPeerContext peer);

   /// <summary>
   /// Adds the addresses of the peers to the address book.
   /// </summary>
   /// <param name="peers">The peers to add.</param>
   void AddAddresses(IEnumerable<IPeerContext> peers);

   /// <summary>
   /// Removes the address of the peer from the address book.
   /// </summary>
   /// <param name="peer">The peer to remove.</param>
   void RemoveAddress(IPeerContext peer);

   /// <summary>
   /// Removes the addresses of the peers from the address book.
   /// </summary>
   /// <param name="peers">The peers to remove.</param>
   void RemoveAddresses(IEnumerable<IPeerContext> peers);

   /// <summary>
   /// Bans the specified peer.
   /// </summary>
   /// <param name="peer">The peer to ban.</param>
   /// <param name="until">The expiration ban date.</param>
   /// <param name="reason">The ban reason.</param>
   void Ban(IPeerContext peer, DateTimeOffset until, string reason);

   /// <summary>
   /// Determines whether the specified peer is banned.
   /// </summary>
   /// <param name="peer">The peer.</param>
   /// <returns>
   ///   <c>true</c> if the specified peer is banned; otherwise, <c>false</c>.
   /// </returns>
   bool IsBanned(IPeerContext peer);
}

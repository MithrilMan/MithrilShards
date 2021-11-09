using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Network.PeerAddressBook;

public class DefaultPeerAddressBook : IPeerAddressBook
{
   private readonly ILogger<DefaultPeerAddressBook> _logger;


   public DefaultPeerAddressBook(ILogger<DefaultPeerAddressBook> logger)
   {
      _logger = logger;
   }

   /// <inheritdoc/>
   public void AddAddress(IPeerContext peer)
   {
      LogNotImplementedWarning();
   }

   /// <inheritdoc/>
   public void AddAddresses(IEnumerable<IPeerContext> peers)
   {
      LogNotImplementedWarning();
   }

   /// <inheritdoc/>
   public void Ban(IPeerContext peer, DateTimeOffset until, string reason)
   {
      _logger.LogDebug("Banning peer {RemoteEndPoint}: {BanReason}", peer.RemoteEndPoint, reason);
      LogNotImplementedWarning();
   }


   /// <inheritdoc/>
   public void RemoveAddress(IPeerContext peer)
   {
      LogNotImplementedWarning();
   }

   /// <inheritdoc/>
   public void RemoveAddresses(IEnumerable<IPeerContext> peers)
   {
      LogNotImplementedWarning();
   }

   /// <inheritdoc/>
   public bool IsBanned(IPeerContext peer)
   {
      LogNotImplementedWarning();

      return false;
   }

   private void LogNotImplementedWarning([CallerMemberName] string methodName = null!)
   {
      _logger.LogWarning($"{methodName} not implemented ");
   }
}

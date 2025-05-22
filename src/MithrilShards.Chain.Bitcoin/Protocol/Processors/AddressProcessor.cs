using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors;

using MithrilShards.Chain.Bitcoin.Network; // Required for BitcoinPeerContext

public class AddressProcessor : BaseProcessor,
   INetworkMessageHandler<GetAddrMessage>,
   INetworkMessageHandler<AddrMessage>,
   INetworkMessageHandler<Addrv2Message> // Task 1.2: Added Addrv2Message handler
{
   // TODO: Inject an actual PeerAddressBook or similar service to store/retrieve addresses.
   // For now, we'll use a placeholder for sending addresses.
   // private readonly IPeerAddressBook _peerAddressBook;

   public AddressProcessor(ILogger<AddressProcessor> logger, IEventBus eventBus, IPeerBehaviorManager peerBehaviorManager) //, IPeerAddressBook peerAddressBook)
      : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true, receiveMessagesOnlyIfHandshaked: true)
   {
      // _peerAddressBook = peerAddressBook;
   }

   protected override async ValueTask OnPeerHandshakedAsync()
   {
      // ask for addresses when the peer handshakes
      // V31402 is for 'addr' with time. 'getaddr' itself is older.
      // No specific version requirement for GetAddrMessage itself, but good practice to send it to peers that support relaying addresses.
      await SendMessageAsync(new GetAddrMessage()).ConfigureAwait(false);

      /// TODO: add a IPeriodicWork that from time to time advertise our peer address and other peer addresses.
      /// bitcoin core has this code in SendMessages:
      /// https://github.com/bitcoin/bitcoin/blob/c7ebab12f9419e7d1622494cbb6578302601c7db/src/net_processing.cpp#L3890-L3927
   }

   public async ValueTask<bool> ProcessMessageAsync(GetAddrMessage message, CancellationToken cancellation)
   {
      logger.LogDebug("Peer sent GetAddrMessage.");

      if (PeerContext is BitcoinPeerContext bitcoinPeerContext && bitcoinPeerContext.SupportsAddrv2)
      {
         logger.LogDebug("Peer supports addrv2. Sending Addrv2Message.");
         // TODO: Fetch actual addresses from PeerAddressBook, converting them to NetworkAddressV2 format.
         var addressesV2 = new System.Collections.Generic.List<NetworkAddressV2>();
         // Example:
         // if (_peerAddressBook.TryGetRandomAddresses(10, out var knownPeers))
         // {
         //    foreach(var peer in knownPeers) { addressesV2.Add(ConvertToNetworkAddressV2(peer)); }
         // }
         await SendMessageAsync(new Addrv2Message { Addresses = addressesV2 }).ConfigureAwait(false);
      }
      else
      {
         logger.LogDebug("Peer does not support addrv2 or context is not BitcoinPeerContext. Sending legacy AddrMessage.");
         // TODO: Fetch actual addresses from PeerAddressBook, converting them to NetworkAddress format.
         var addressesV1 = Array.Empty<NetworkAddress>();
         // Example:
         // if (_peerAddressBook.TryGetRandomAddresses(10, out var knownPeers))
         // {
         //    addressesV1 = knownPeers.Select(p => ConvertToNetworkAddress(p)).ToArray();
         // }
         await SendMessageAsync(new AddrMessage { Addresses = addressesV1 }).ConfigureAwait(false);
      }
      return true; // Message handled
   }

   ValueTask<bool> INetworkMessageHandler<AddrMessage>.ProcessMessageAsync(AddrMessage message, CancellationToken cancellation)
   {
      logger.LogDebug("Received AddrMessage with {Count} addresses.", message.Addresses?.Length ?? 0);
      if (message.Addresses != null)
      {
         foreach (NetworkAddress addr in message.Addresses)
         {
            // TODO: Process and store these addresses in PeerAddressBook.
            // For now, just log them.
            logger.LogTrace("Legacy Addr: {Address}:{Port} (Services: {Services}, Time: {Time})",
                            addr.EndPoint?.Address?.ToString() ?? "N/A",
                            addr.EndPoint?.Port ?? 0,
                            (NodeServices)addr.Services,
                            DateTimeOffset.FromUnixTimeSeconds(addr.Timestamp).ToString("u"));
         }
      }
      return new ValueTask<bool>(true); // Message handled
   }

   // Task 1.2: Handler for Addrv2Message
   ValueTask<bool> INetworkMessageHandler<Addrv2Message>.ProcessMessageAsync(Addrv2Message message, CancellationToken cancellation)
   {
      logger.LogDebug("Received Addrv2Message with {Count} addresses.", message.Addresses?.Count ?? 0);
      if (message.Addresses != null)
      {
         foreach (NetworkAddressV2 addrV2 in message.Addresses)
         {
            // TODO: Process and store these addresses in PeerAddressBook.
            // For now, just log them.
            string addressString = BitConverter.ToString(addrV2.Address);
            try
            {
               if (addrV2.NetworkId == BIP155NetworkId.IPV4 && addrV2.Address.Length == 4)
               {
                  addressString = new System.Net.IPAddress(addrV2.Address).ToString();
               }
               else if (addrV2.NetworkId == BIP155NetworkId.IPV6 && addrV2.Address.Length == 16)
               {
                  addressString = new System.Net.IPAddress(addrV2.Address).ToString();
               }
            }
            catch { /* Ignore parsing errors for logging */ }

            logger.LogTrace("Addrv2: NetID {NetworkId}, Addr {Address}:{Port} (Services: {Services}, Time: {Time})",
                            addrV2.NetworkId,
                            addressString,
                            addrV2.Port,
                            addrV2.Services,
                            DateTimeOffset.FromUnixTimeSeconds(addrV2.Time).ToString("u"));
         }
      }
      return new ValueTask<bool>(true); // Message handled
   }
}

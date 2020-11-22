﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public class AddressProcessor : BaseProcessor,
      INetworkMessageHandler<GetAddrMessage>,
      INetworkMessageHandler<AddrMessage>
   {

      public AddressProcessor(ILogger<AddressProcessor> logger, IEventBus eventBus, IPeerBehaviorManager peerBehaviorManager)
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true, receiveMessagesOnlyIfHandshaked: true)
      {
      }

      protected override async ValueTask OnPeerHandshakedAsync()
      {
         // ask for addresses when the peer handshakes
         await this.SendMessageAsync(minVersion: KnownVersion.V31402, new GetAddrMessage()).ConfigureAwait(false);

         /// TODO: add a timer that from time to time advertise our peer address and other peer addresses.
         /// bitcoin core has this code in SendMessages:
         /// https://github.com/bitcoin/bitcoin/blob/c7ebab12f9419e7d1622494cbb6578302601c7db/src/net_processing.cpp#L3890-L3927
      }

      public async ValueTask<bool> ProcessMessageAsync(GetAddrMessage message, CancellationToken cancellation)
      {
         this.logger.LogDebug("Peer requiring addresses from us.");
         NetworkAddress[] fetchedAddresses = Array.Empty<NetworkAddress>(); //TODO fetch addresses from addressmananager
         await this.SendMessageAsync(new AddrMessage { Addresses = fetchedAddresses }).ConfigureAwait(false);
         return true;
      }

      public ValueTask<bool> ProcessMessageAsync(AddrMessage message, CancellationToken cancellation)
      {
         this.logger.LogDebug("Peer sent us a list of addresses.");
         //TODO
         return new ValueTask<bool>(true);
      }
   }
}
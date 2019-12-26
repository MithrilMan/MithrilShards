using System;
using System.Collections.Generic;
using System.Text;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Protocol {
   public class HandshakeVersionFactory {
      readonly IRandomNumberGenerator randomNumberGenerator;
      readonly IDateTimeProvider dateTimeProvider;

      public HandshakeVersionFactory(IRandomNumberGenerator randomNumberGenerator, IDateTimeProvider dateTimeProvider) {
         this.randomNumberGenerator = randomNumberGenerator;
         this.dateTimeProvider = dateTimeProvider;
      }

      public VersionMessage Create(IPeerContext peerContext) {
         var version = new VersionMessage() {
            Nonce = this.randomNumberGenerator.GetUint64(),
            UserAgent = "MithrilShards.Forge",
            Version = KnownVersion.CurrentVersion,
            Timestamp = this.dateTimeProvider.GetTimeOffset(),
            ReceiverAddress = new Serialization.Types.NetworkAddress(true) {
               EndPoint = peerContext.RemoteEndPoint,
            },
            SenderAddress = new Serialization.Types.NetworkAddress(true) {
               EndPoint = peerContext.PublicEndPoint,
            },
            Relay = true, //this.IsRelay, TODO: it's part of the node settings
            Services = (ulong)NodeServices.Network // TODO: it's part of the node settings and depends on the configured features/shards
         };

         return version;
      }
   }
}

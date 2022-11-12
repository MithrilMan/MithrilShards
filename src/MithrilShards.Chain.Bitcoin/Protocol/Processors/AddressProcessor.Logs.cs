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

public partial class AddressProcessor
{
   [LoggerMessage(0, LogLevel.Debug, "Peer requiring addresses from us.")]
   partial void DebugAddressesRequested();

   [LoggerMessage(0, LogLevel.Debug, "Peer sent us a list of addresses.")]
   partial void DebugAddressesSent();
}

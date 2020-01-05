using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public class AddressProcessor : BaseProcessor,
      INetworkMessageHandler<GetAddrMessage>,
      INetworkMessageHandler<AddrMessage>
   {

      public AddressProcessor(ILogger<HandshakeProcessor> logger, IEventBus eventBus)
         : base(logger, eventBus)
      {
      }

      public async ValueTask<bool> ProcessMessageAsync(GetAddrMessage message, CancellationToken cancellation)
      {
         this.logger.LogDebug("Peer requiring addresses from us.");
         NetworkAddress[] fetchedAddresses = null; //here fetch addresses from addressmananager
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
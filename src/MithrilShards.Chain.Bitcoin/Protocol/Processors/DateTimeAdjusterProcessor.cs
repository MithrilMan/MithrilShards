using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   /// <summary>
   /// Manage the time drift between peers in order to have a median time to use as reference for consensus rules checks and node behavior.
   /// </summary>
   /// <seealso cref="MithrilShards.Chain.Bitcoin.Protocol.Processors.BaseProcessor" />
   public partial class DateTimeAdjusterProcessor : BaseProcessor
   {
      readonly IDateTimeProvider _dateTimeProvider;

      public DateTimeAdjusterProcessor(ILogger<DateTimeAdjusterProcessor> logger,
                                  IEventBus eventBus,
                                  IPeerBehaviorManager peerBehaviorManager,
                                  IDateTimeProvider dateTimeProvider
                                 )
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true, receiveMessagesOnlyIfHandshaked: true)
      {
         this._dateTimeProvider = dateTimeProvider;
      }

      /// <summary>
      /// When the peer handshake, sends <see cref="SendCmpctMessage"/>  and <see cref="SendHeadersMessage"/> if the
      /// negotiated protocol allow that and update peer status based on its version message.
      /// </summary>
      /// <param name="event">The event.</param>
      /// <returns></returns>
      protected override ValueTask OnPeerHandshakedAsync()
      {
         this._dateTimeProvider.AddTimeData(this.PeerContext.TimeOffset, this.PeerContext.RemoteEndPoint);

         return default;
      }
   }
}
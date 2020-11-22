using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// temporary Mock, to be moved and implemented properly
   /// </summary>
   public class InitialBlockDownloadTracker : IInitialBlockDownloadTracker
   {
      readonly ILogger<InitialBlockDownloadTracker> _logger;
      readonly IEventBus _eventBus;
      readonly IChainState _chainState;
      readonly IConsensusParameters _consensusParameters;
      readonly IDateTimeProvider _dateTimeProvider;
      private readonly Target? _minimumChainWork;
      private readonly long _maxTipAge;
      readonly EventSubscriptionManager _subscriptionManager = new EventSubscriptionManager();

      public InitialBlockDownloadTracker(ILogger<InitialBlockDownloadTracker> logger,
                                         IEventBus eventBus,
                                         IChainState chainState,
                                         IConsensusParameters consensusParameters,
                                         IOptions<BitcoinSettings> options,
                                         IDateTimeProvider dateTimeProvider)
      {
         this._logger = logger;
         this._eventBus = eventBus;
         this._chainState = chainState;
         this._consensusParameters = consensusParameters;
         this._dateTimeProvider = dateTimeProvider;

         //TODO register to tip advance
         //this.subscriptionManager.RegisterSubscriptions(this.eventBus.Subscribe())

         _minimumChainWork = options.Value.MinimumChainWork ?? this._consensusParameters.MinimumChainWork;
         if (_minimumChainWork < this._consensusParameters.MinimumChainWork)
         {
            this._logger.LogWarning($"{nameof(_minimumChainWork)} set below default value of {this._consensusParameters.MinimumChainWork}");
         }

         this._maxTipAge = options.Value.MaxTipAge;
      }

      public bool IsDownloadingBlocks()
      {
         return this._chainState.ChainTip.ChainWork < _minimumChainWork
            || (this._chainState.GetTipHeader().TimeStamp < (this._dateTimeProvider.GetTime() - this._maxTipAge));
      }
   }
}

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
      readonly ILogger<InitialBlockDownloadTracker> logger;
      readonly IEventBus eventBus;
      readonly IChainState chainState;
      readonly IConsensusParameters consensusParameters;
      readonly IDateTimeProvider dateTimeProvider;
      private Target? minimumChainWork;
      private long maxTipAge;
      readonly EventSubscriptionManager subscriptionManager = new EventSubscriptionManager();

      public InitialBlockDownloadTracker(ILogger<InitialBlockDownloadTracker> logger,
                                         IEventBus eventBus,
                                         IChainState chainState,
                                         IConsensusParameters consensusParameters,
                                         IOptions<BitcoinSettings> options,
                                         IDateTimeProvider dateTimeProvider)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.chainState = chainState;
         this.consensusParameters = consensusParameters;
         this.dateTimeProvider = dateTimeProvider;

         //TODO register to tip advance
         //this.subscriptionManager.RegisterSubscriptions(this.eventBus.Subscribe())

         minimumChainWork = options.Value.MinimumChainWork ?? this.consensusParameters.MinimumChainWork;
         if (minimumChainWork < this.consensusParameters.MinimumChainWork)
         {
            this.logger.LogWarning($"{nameof(minimumChainWork)} set below default value of {this.consensusParameters.MinimumChainWork}");
         }

         this.maxTipAge = options.Value.MaxTipAge;
      }

      public bool IsDownloadingBlocks()
      {
         return this.chainState.ChainTip.ChainWork < minimumChainWork
            || (this.chainState.GetTipHeader().TimeStamp < (this.dateTimeProvider.GetTime() - this.maxTipAge));
      }
   }
}

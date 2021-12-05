using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Chain.Bitcoin.Consensus;

/// <summary>
/// temporary Mock, to be moved and implemented properly
/// </summary>
public partial class InitialBlockDownloadTracker : IInitialBlockDownloadTracker
{
   readonly IEventBus _eventBus;
   readonly IChainState _chainState;
   readonly IConsensusParameters _consensusParameters;
   readonly IDateTimeProvider _dateTimeProvider;
   private readonly Target? _minimumChainWork;
   private readonly long _maxTipAge;
   readonly EventSubscriptionManager _subscriptionManager = new();

   public InitialBlockDownloadTracker(ILogger<InitialBlockDownloadTracker> logger,
                                      IEventBus eventBus,
                                      IChainState chainState,
                                      IConsensusParameters consensusParameters,
                                      IOptions<BitcoinSettings> options,
                                      IDateTimeProvider dateTimeProvider)
   {
      _logger = logger;
      _eventBus = eventBus;
      _chainState = chainState;
      _consensusParameters = consensusParameters;
      _dateTimeProvider = dateTimeProvider;

      //TODO register to tip advance
      //this.subscriptionManager.RegisterSubscriptions(this.eventBus.Subscribe())

      _minimumChainWork = options.Value.MinimumChainWork ?? _consensusParameters.MinimumChainWork;
      if (_minimumChainWork < _consensusParameters.MinimumChainWork)
      {
         WarningChainworkSetLowerThanConsensusChainwork(_minimumChainWork, _consensusParameters.MinimumChainWork);
      }

      _maxTipAge = options.Value.MaxTipAge;
   }

   public bool IsDownloadingBlocks()
   {
      return _chainState.ChainTip.ChainWork < _minimumChainWork
         || (_chainState.GetTipHeader().TimeStamp < (_dateTimeProvider.GetTime() - _maxTipAge));
   }
}

public partial class InitialBlockDownloadTracker
{
   readonly ILogger<InitialBlockDownloadTracker> _logger;

   [LoggerMessage(0, LogLevel.Warning, "{CustomMinimumChainWork} set below default value of {ConsensusMinimumChainWork}.")]
   partial void WarningChainworkSetLowerThanConsensusChainwork(Target customMinimumChainWork, Target ConsensusMinimumChainWork);
}
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Core.EventBus;

namespace MithrilShards.Chain.Bitcoin.Consensus;

/// <summary>
/// temporary Mock, to be moved and implemented properly
/// </summary>
public class InitialBlockDownloadTracker : IInitialBlockDownloadTracker
{
   readonly ILogger<InitialBlockDownloadTracker> _logger;
   readonly IChainState _chainState;
   readonly IConsensusParameters _consensusParameters;
   readonly IDateTimeProvider _dateTimeProvider;
   private readonly Target? _minimumChainWork;
   private readonly long _maxTipAge;

   public InitialBlockDownloadTracker(ILogger<InitialBlockDownloadTracker> logger,
                                      IChainState chainState,
                                      IConsensusParameters consensusParameters,
                                      IOptions<BitcoinSettings> options,
                                      IDateTimeProvider dateTimeProvider)
   {
      _logger = logger;
      _chainState = chainState;
      _consensusParameters = consensusParameters;
      _dateTimeProvider = dateTimeProvider;

      //TODO register to tip advance
      //this.subscriptionManager.RegisterSubscriptions(this.eventBus.Subscribe())

      _minimumChainWork = options.Value.MinimumChainWork ?? _consensusParameters.MinimumChainWork;
      if (_minimumChainWork < _consensusParameters.MinimumChainWork)
      {
         _logger.LogWarning("_minimumChainWork set below default value of {DefaultMinimumChainWork}", _consensusParameters.MinimumChainWork);
      }

      _maxTipAge = options.Value.MaxTipAge;
   }

   public bool IsDownloadingBlocks()
   {
      return _chainState.ChainTip.ChainWork < _minimumChainWork
         || (_chainState.GetTipHeader().TimeStamp < (_dateTimeProvider.GetTime() - _maxTipAge));
   }
}

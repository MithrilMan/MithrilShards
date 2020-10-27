using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{
   public interface IChainDefinition
   {
      INetworkDefinition NetworkDefinition { get; }

      IConsensusParameters Consensus { get;}
   }
}
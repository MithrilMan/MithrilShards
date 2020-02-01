using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{
   public abstract class BitcoinChain : IChainDefinition
   {
      public INetworkDefinition NetworkDefinition { get; private set; } = null!;

      public IConsensusParameters Consensus { get; private set; } = null!;


      public BitcoinChain()
      {
         this.Initialize();
      }

      protected void Initialize()
      {
         this.NetworkDefinition = this.ConfigureNetwork();
         this.Consensus = this.ConfigureConsensus();
      }

      public abstract BitcoinNetworkDefinition ConfigureNetwork();

      public abstract ConsensusParameters ConfigureConsensus();
   }
}

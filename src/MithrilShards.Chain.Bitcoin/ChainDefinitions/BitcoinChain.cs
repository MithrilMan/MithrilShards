using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions;

public abstract class BitcoinChain : IChainDefinition
{
   protected const long COIN = 100_000_000;
   readonly IBlockHeaderHashCalculator _blockHeaderHashCalculator;

   public INetworkDefinition NetworkDefinition { get; private set; } = null!;

   public IConsensusParameters Consensus { get; private set; } = null!;


   public BitcoinChain(IBlockHeaderHashCalculator blockHeaderHashCalculator)
   {
      _blockHeaderHashCalculator = blockHeaderHashCalculator;

      Initialize();
   }

   protected void Initialize()
   {
      NetworkDefinition = ConfigureNetwork();
      Consensus = ConfigureConsensus();
   }

   public abstract BitcoinNetworkDefinition ConfigureNetwork();

   public abstract ConsensusParameters ConfigureConsensus();

   protected UInt256 ComputeHash(BlockHeader header)
   {
      return _blockHeaderHashCalculator.ComputeHash(header, protocolVersion: 0); //protocol version doesn't matter for hash
   }
}

using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers;
using MithrilShards.Core.Memory;
using MithrilShards.Core.Network.Protocol.Serialization;


namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Rules;

public class CheckSize : IBlockValidationRule
{
   readonly ILogger<CheckSize> _logger;
   readonly IProtocolTypeSerializer<Protocol.Types.Block> _blockSerializer;
   readonly IConsensusParameters _consensusParameters;

   public CheckSize(ILogger<CheckSize> logger, IProtocolTypeSerializer<Protocol.Types.Block> blockSerializer, IConsensusParameters consensusParameters)
   {
      _logger = logger;
      _blockSerializer = blockSerializer;
      _consensusParameters = consensusParameters;
   }


   public bool Check(IBlockValidationContext context, ref BlockValidationState validationState)
   {
      int transactionsCount = context.Block.Transactions!.Length;

      if (
         transactionsCount == 0
         || transactionsCount * _consensusParameters.WitnessScaleFactor > _consensusParameters.MaxBlockWeight
         || GetBlockSize(context.Block) * _consensusParameters.WitnessScaleFactor > _consensusParameters.MaxBlockWeight
         )
      {
         return validationState.Invalid(BlockValidationStateResults.Consensus, "bad-blk-length", "size limits failed");
      }

      return true;
   }

   private int GetBlockSize(Protocol.Types.Block block)
   {
      return _blockSerializer.Serialize(
         block,
         KnownVersion.CurrentVersion,
         new PooledByteBufferWriter(block.Transactions!.Length * 256),
         new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, false))
         );
   }
}

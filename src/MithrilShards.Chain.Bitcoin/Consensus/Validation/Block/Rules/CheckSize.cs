using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers;
using MithrilShards.Core.Memory;
using MithrilShards.Core.Network.Protocol.Serialization;


namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block.Rules
{
   public class CheckSize : IBlockValidationRule
   {
      readonly ILogger<CheckSize> logger;
      readonly IProtocolTypeSerializer<Protocol.Types.Block> blockSerializer;
      readonly IConsensusParameters consensusParameters;

      public CheckSize(ILogger<CheckSize> logger, IProtocolTypeSerializer<Protocol.Types.Block> blockSerializer, IConsensusParameters consensusParameters)
      {
         this.logger = logger;
         this.blockSerializer = blockSerializer;
         this.consensusParameters = consensusParameters;
      }


      public bool Check(IBlockValidationContext context, ref BlockValidationState validationState)
      {
         int transactionsCount = context.Block.Transactions!.Length;

         if (
            transactionsCount == 0
            || transactionsCount * consensusParameters.WitnessScaleFactor > consensusParameters.MaxBlockWeight
            || GetBlockSize(context.Block) * consensusParameters.WitnessScaleFactor > consensusParameters.MaxBlockWeight
            )
         {
            return validationState.Invalid(BlockValidationStateResults.Consensus, "bad-blk-length", "size limits failed");
         }

         return true;
      }

      private int GetBlockSize(Protocol.Types.Block block)
      {
         PooledByteBufferWriter buffer = new PooledByteBufferWriter(block.Transactions!.Length * 256);

         return this.blockSerializer.Serialize(
            block,
            KnownVersion.CurrentVersion,
            new PooledByteBufferWriter(block.Transactions!.Length * 256),
            new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, false))
            );
      }
   }
}
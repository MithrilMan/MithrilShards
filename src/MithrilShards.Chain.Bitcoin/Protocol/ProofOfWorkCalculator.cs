using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   /// <summary>
   /// Methods to compute required work (PoW)
   /// </summary>
   public class ProofOfWorkCalculator : IProofOfWorkCalculator
   {
      readonly ILogger<ProofOfWorkCalculator> _logger;
      readonly IConsensusParameters _consensusParameters;
      readonly IBlockHeaderRepository _blockHeaderRepository;

      public ProofOfWorkCalculator(ILogger<ProofOfWorkCalculator> logger,
                                   IConsensusParameters consensusParameters,
                                   IBlockHeaderRepository blockHeaderRepository)
      {
         this._logger = logger;
         this._consensusParameters = consensusParameters;
         this._blockHeaderRepository = blockHeaderRepository;
      }

      public uint GetNextWorkRequired(HeaderNode previousHeaderNode, BlockHeader header)
      {
         if (previousHeaderNode == null) ThrowHelper.ThrowArgumentNullException(nameof(previousHeaderNode));

         if (!this._blockHeaderRepository.TryGet(previousHeaderNode.Hash, out BlockHeader? previousHeader))
         {
            //this should never happens, if it happens means we have consistency problem (we lost an header)
            ThrowHelper.ThrowArgumentNullException(nameof(previousHeaderNode));
         }

         uint proofOfWorkLimit = this._consensusParameters.PowLimit.ToCompact();
         int difficultyAdjustmentInterval = (int)this.GetDifficultyAdjustmentInterval();

         // Only change once per difficulty adjustment interval
         if ((previousHeaderNode.Height + 1) % difficultyAdjustmentInterval != 0)
         {
            if (this._consensusParameters.PowAllowMinDifficultyBlocks)
            {
               /// Special difficulty rule for test networks:
               /// if the new block's timestamp is more than 2 times the PoWTargetSpacing then allow mining of a min-difficulty block.
               if (header.TimeStamp > (previousHeader.TimeStamp + (this._consensusParameters.PowTargetSpacing * 2)))
               {
                  return proofOfWorkLimit;
               }
               else
               {
                  // Return the last non-special-min-difficulty-rules-block.
                  HeaderNode currentHeaderNode = previousHeaderNode;
                  BlockHeader currentHeader = previousHeader;
                  while (currentHeaderNode.Previous != null
                     && (currentHeaderNode.Height % difficultyAdjustmentInterval) != 0
                     && this._blockHeaderRepository.TryGet(currentHeaderNode.Hash, out currentHeader!) && currentHeader.Bits == proofOfWorkLimit
                     )
                  {
                     currentHeaderNode = currentHeaderNode.Previous;
                  }

                  return currentHeader.Bits;
               }
            }

            return previousHeader.Bits;
         }

         // Go back by what we want to be 14 days worth of blocks
         int heightReference = previousHeaderNode.Height - (difficultyAdjustmentInterval - 1);
         HeaderNode? headerNodeReference = previousHeaderNode.GetAncestor(heightReference);

         BlockHeader? headerReference = null;
         if (headerNodeReference == null || !this._blockHeaderRepository.TryGet(headerNodeReference.Hash, out headerReference))
         {
            ThrowHelper.ThrowNotSupportedException("Header ancestor not found, PoW required work computation requires a full chain.");
         }

         return this.CalculateNextWorkRequired(previousHeader, headerReference.TimeStamp);
      }

      public uint CalculateNextWorkRequired(BlockHeader previousHeader, uint timeReference)
      {
         if (this._consensusParameters.PowNoRetargeting)
         {
            return previousHeader.Bits;
         }

         // Limit adjustment step
         uint actualTimespan = previousHeader.TimeStamp - timeReference;
         if (actualTimespan < this._consensusParameters.PowTargetTimespan / 4)
         {
            actualTimespan = this._consensusParameters.PowTargetTimespan / 4;
         }
         else if (actualTimespan > this._consensusParameters.PowTargetTimespan * 4)
         {
            actualTimespan = this._consensusParameters.PowTargetTimespan * 4;
         }

         // retarget
         var bnNew = new Target(previousHeader.Bits);
         bnNew.Multiply(actualTimespan);
         bnNew.Divide(this._consensusParameters.PowTargetTimespan);

         if (bnNew > this._consensusParameters.PowLimit)
         {
            bnNew = this._consensusParameters.PowLimit;
         }

         return bnNew.ToCompact();
      }


      /// <summary>
      /// Calculate the difficulty adjustment interval in blocks based on settings defined in <see cref="IConsensus"/>.
      /// </summary>
      /// <returns>The difficulty adjustment interval in blocks.</returns>
      private long GetDifficultyAdjustmentInterval()
      {
         return this._consensusParameters.PowTargetTimespan / this._consensusParameters.PowTargetSpacing;
      }


      public bool CheckProofOfWork(BlockHeader header)
      {
         var blockTarget = new Target(header.Bits, out bool isNegative, out bool isOverflow);

         // check range
         if (isNegative || blockTarget == Target.Zero || isOverflow || blockTarget > this._consensusParameters.PowLimit)
         {
            return false;
         }

         // Check proof of work matches claimed amount
         if (header.Hash > blockTarget)
         {
            return false;
         }

         return true;
      }
   }
}

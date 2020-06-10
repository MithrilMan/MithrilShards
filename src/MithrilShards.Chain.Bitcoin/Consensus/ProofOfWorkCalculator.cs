using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// Methods to compute required work (PoW)
   /// </summary>
   public class ProofOfWorkCalculator : IProofOfWorkCalculator
   {
      readonly ILogger<ProofOfWorkCalculator> logger;
      readonly IConsensusParameters consensusParameters;
      readonly HeadersTree headersTree;

      public ProofOfWorkCalculator(ILogger<ProofOfWorkCalculator> logger, IConsensusParameters consensusParameters, HeadersTree headersTree)
      {
         this.logger = logger;
         this.consensusParameters = consensusParameters;
         this.headersTree = headersTree;
      }

      public uint GetNextWorkRequired(HeaderNode previousHeaderNode, BlockHeader header)
      {
         if (previousHeaderNode == null) ThrowHelper.ThrowArgumentNullException(nameof(previousHeaderNode));

         if (!this.headersTree.TryGetBlockHeader(previousHeaderNode, out BlockHeader? previousHeader))
         {
            ThrowHelper.ThrowArgumentNullException(nameof(previousHeaderNode));
         }

         uint proofOfWorkLimit = this.consensusParameters.PowLimit.ToCompact();
         int difficultyAdjustmentInterval = (int)this.GetDifficultyAdjustmentInterval();

         // Only change once per difficulty adjustment interval
         if ((previousHeaderNode.Height + 1) % difficultyAdjustmentInterval != 0)
         {
            if (this.consensusParameters.PowAllowMinDifficultyBlocks)
            {
               /// Special difficulty rule for test networks:
               /// if the new block's timestamp is more than 2 times the PoWTargetSpacing then allow mining of a min-difficulty block.
               if (header.TimeStamp > (previousHeader.TimeStamp + (this.consensusParameters.PowTargetSpacing * 2)))
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
                     && this.headersTree.TryGetBlockHeader(currentHeaderNode, out currentHeader!) && currentHeader.Bits == proofOfWorkLimit
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

         if (!this.headersTree.TryGetBlockHeader(headerNodeReference, out BlockHeader? headerReference))
         {
            ThrowHelper.ThrowNotSupportedException("Header ancestor not found, PoW required work computation requires a full chain.");
         }

         return this.CalculateNextWorkRequired(previousHeaderNode, previousHeader, headerReference.TimeStamp);
      }

      public uint CalculateNextWorkRequired(HeaderNode previousHeaderNode, BlockHeader previousHeader, long timeReference)
      {
         if (this.consensusParameters.PowNoRetargeting)
         {
            return previousHeader.Bits;
         }

         // Limit adjustment step
         ulong actualTimespan = (ulong)Math.Clamp(
             value: previousHeader.TimeStamp - timeReference,
             min: this.consensusParameters.PowTargetTimespan / 4,
             max: this.consensusParameters.PowTargetTimespan * 4
             );

         // retarget
         Target powLimit = this.consensusParameters.PowLimit;
         Target bnNew = new Target(previousHeader.Bits) * actualTimespan / (ulong)this.consensusParameters.PowTargetTimespan;

         if (bnNew > powLimit)
         {
            bnNew = powLimit;
         }

         return bnNew.ToCompact();
      }


      /// <summary>
      /// Calculate the difficulty adjustment interval in blocks based on settings defined in <see cref="IConsensus"/>.
      /// </summary>
      /// <returns>The difficulty adjustment interval in blocks.</returns>
      private long GetDifficultyAdjustmentInterval()
      {
         return this.consensusParameters.PowTargetTimespan / this.consensusParameters.PowTargetSpacing;
      }
   }
}

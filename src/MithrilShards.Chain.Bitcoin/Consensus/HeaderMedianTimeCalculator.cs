using System;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public class HeaderMedianTimeCalculator : IHeaderMedianTimeCalculator
   {
      /// <summary>Header length for calculating median timespan.</summary>
      private const int medianTimeSpan = 11;
      readonly ILogger<HeaderMedianTimeCalculator> logger;
      readonly IBlockHeaderRepository blockHeaderRepository;

      public HeaderMedianTimeCalculator(ILogger<HeaderMedianTimeCalculator> logger, IBlockHeaderRepository blockHeaderRepository)
      {
         this.logger = logger;
         this.blockHeaderRepository = blockHeaderRepository;
      }

      /// <summary>
      /// Calculate (backward) the median block time over <see cref="medianTimeSpan" /> window from this entry in the chain.
      /// </summary>
      /// <param name="startingBlockHash">The starting block hash.</param>
      /// <param name="startingBlockHeight">Height of the starting block.</param>
      /// <returns>
      /// The median block time.
      /// </returns>
      public uint Calculate(UInt256 startingBlockHash, int startingBlockHeight)
      {
         if (startingBlockHeight <= 0)
         {
            ThrowHelper.ThrowArgumentException("{startingBlockHeight} must be greater than 0");
         }

         int samplesLenght = startingBlockHeight > medianTimeSpan ? medianTimeSpan : startingBlockHeight + 1;
         var median = new uint[samplesLenght];

         if (!this.blockHeaderRepository.TryGet(startingBlockHash, out BlockHeader? currentHeader))
         {
            ThrowHelper.ThrowNotSupportedException("Fatal exception, shouldn't happen, repository may be corrupted.");
         }

         for (int i = samplesLenght - 1; i > 0; i--)
         {
            median[i] = currentHeader.TimeStamp;

            if (!this.blockHeaderRepository.TryGet(currentHeader.PreviousBlockHash!, out currentHeader))
            {
               ThrowHelper.ThrowNotSupportedException("Fatal exception, shouldn't happen, repository may be corrupted.");
            }
         }

         median[0] = currentHeader.TimeStamp;

         Array.Sort(median);
         return median[samplesLenght / 2];
      }
   }
}

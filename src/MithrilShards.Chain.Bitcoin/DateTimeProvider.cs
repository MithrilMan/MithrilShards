using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Extensions;

namespace MithrilShards.Chain.Bitcoin
{
   public class DateTimeProvider : IDateTimeProvider
   {
      const int MAX_SAMPLES = 200;
      const string WARNING_MESSAGE = @"
**************************************************************
Please check that your computer's date and time are correct!
If your clock is wrong, the node will not work properly.
To recover from this state fix your time and restart the node.
**************************************************************
";

      readonly ILogger<DateTimeProvider> logger;
      private readonly BitcoinSettings settings;


      private readonly MedianFilter<long> medianFilter;
      private readonly HashSet<IPAddress> knownPeers;
      private bool autoAdjustingTimeEnabled = true;
      private bool showWarning = false;

      /// <summary>UTC adjusted timestamp, or null if no adjusted time is set.</summary>
      protected TimeSpan adjustedTimeOffset { get; set; }

      /// <summary>
      /// Initializes instance of the object.
      /// </summary>
      public DateTimeProvider(ILogger<DateTimeProvider> logger, IOptions<BitcoinSettings> options)
      {
         this.logger = logger;
         this.settings = options.Value;

         this.adjustedTimeOffset = TimeSpan.Zero;

         this.medianFilter = new MedianFilter<long>(
            size: MAX_SAMPLES,
            initialValue: 0,
            medianComputationOnEvenElements: args => (args.lowerItem + args.higherItem) / 2
            );

         this.knownPeers = new HashSet<IPAddress>(MAX_SAMPLES);
      }

      /// <inheritdoc />
      public virtual long GetTime()
      {
         return DateTime.UtcNow.ToUnixTimestamp();
      }

      /// <inheritdoc />
      public virtual DateTime GetUtcNow()
      {
         return DateTime.UtcNow;
      }

      /// <inheritdoc />
      public virtual DateTimeOffset GetTimeOffset()
      {
         return DateTimeOffset.UtcNow;
      }

      /// <inheritdoc />
      public DateTime GetAdjustedTime()
      {
         return this.GetUtcNow().Add(this.adjustedTimeOffset);
      }

      /// <inheritdoc />
      public long GetAdjustedTimeAsUnixTimestamp()
      {
         return new DateTimeOffset(this.GetAdjustedTime()).ToUnixTimeSeconds();
      }

      /// <inheritdoc />
      public void SetAdjustedTimeOffset(TimeSpan adjustedTimeOffset)
      {
         this.adjustedTimeOffset = adjustedTimeOffset;
      }

      public void AddTimeData(TimeSpan timeoffset, IPEndPoint remoteEndPoint)
      {
         if (!autoAdjustingTimeEnabled)
         {
            this.logger.LogDebug("Automatic time adjustment is disabled.");
            if (showWarning)
            {
               this.logger.LogCritical(WARNING_MESSAGE);
            }
            return;
         }

         /// note: this behavior mimic bitcoin core but it's broken as it is on bitcoin core.
         /// something better should be implemented.

         if (this.knownPeers.Count == MAX_SAMPLES)
         {
            this.logger.LogDebug("Ignored AddTimeData: max peer tracked.");
            return;
         }

         if (this.knownPeers.Contains(remoteEndPoint.Address))
         {
            this.logger.LogDebug("Ignored AddTimeData: peer already contributed.");
            return;
         }

         this.knownPeers.Add(remoteEndPoint.Address);
         this.medianFilter.AddSample((long)timeoffset.TotalSeconds);

         // There is a known issue here (see issue #4521):
         //
         // - The structure vTimeOffsets contains up to 200 elements, after which
         // any new element added to it will not increase its size, replacing the
         // oldest element.
         //
         // - The condition to update nTimeOffset includes checking whether the
         // number of elements in vTimeOffsets is odd, which will never happen after
         // there are 200 elements.
         //
         // But in this case the 'bug' is protective against some attacks, and may
         // actually explain why we've never seen attacks which manipulate the
         // clock offset.
         //
         // So we should hold off on fixing this and clean it up as part of
         // a timing cleanup that strengthens it in a number of other ways.
         //
         if (this.medianFilter.Count >= 5 && this.medianFilter.Count % 2 == 1)
         {
            long median = this.medianFilter.GetMedian();

            // Only let other nodes change our time by so much
            if (Math.Abs(median) <= Math.Max(0, this.settings.MaxTimeAdjustment))
            {
               this.SetAdjustedTimeOffset(TimeSpan.FromSeconds(median));
            }
            else
            {
               this.autoAdjustingTimeEnabled = false;
               this.showWarning = true;
               this.SetAdjustedTimeOffset(TimeSpan.Zero);
            }
         }
      }
   }
}

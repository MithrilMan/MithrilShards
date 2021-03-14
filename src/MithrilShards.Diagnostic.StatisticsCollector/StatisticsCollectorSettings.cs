using MithrilShards.Core.Shards;

namespace MithrilShards.Diagnostic.StatisticsCollector
{
   public class StatisticsCollectorSettings : MithrilShardSettingsBase
   {
      /// <summary>
      /// Gets or sets a value indicating whether the stats should be continuously dumped on console.
      /// </summary>
      /// <value>
      ///   <c>true</c> if [continuous display]; otherwise, <c>false</c>.
      /// </value>
      public bool ContinuousConsoleDisplay { get; set; } = true;

      /// <summary>
      /// Gets or sets the continuous console display rate, in seconds, used when <see cref="ContinuousConsoleDisplay"/> is <see langword="true"/>.
      /// </summary>
      /// <value>
      /// The continuous console display rate in seconds.
      /// </value>
      public int ContinuousConsoleDisplayRate { get; set; } = 30;
   }
}
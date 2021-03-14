using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Core.Shards;

namespace MithrilShards.Chain.Bitcoin
{
   public class BitcoinSettings : MithrilShardSettingsBase
   {
      const long DEFAULT_MAX_TIME_ADJUSTMENT = 70 * 60;
      const long DEFAULT_MAX_TIP_AGE = 24 * 60 * 60;

      public long MaxTimeAdjustment { get; set; } = DEFAULT_MAX_TIME_ADJUSTMENT;

      public Target? MinimumChainWork { get; set; } = null;

      /// <summary>
      /// Maximum age of the tip before the node enters IBD mode (Initial Block Download).
      /// </summary>
      public long MaxTipAge { get; set; } = DEFAULT_MAX_TIP_AGE;
   }
}
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Chain.Bitcoin
{

   public class BitcoinSettings : MithrilShardSettingsBase
   {
      const long DEFAULT_MAX_TIME_ADJUSTMENT = 70 * 60;

      public long MaxTimeAdjustment { get; set; } = DEFAULT_MAX_TIME_ADJUSTMENT;

      public Target? MinimumChainWork { get; set; } = null;
   }
}
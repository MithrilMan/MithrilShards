using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Example
{
   public class ExampleSettings : MithrilShardSettingsBase
   {
      const long DEFAULT_PARAMETER_1 = 1;
      const string DEFAULT_PARAMETER_2 = "2";
      const long DEFAULT_MAX_TIME_ADJUSTMENT = 70 * 60;

      public long Parameter1 { get; set; } = DEFAULT_PARAMETER_1;

      public string Parameter2 { get; set; } = DEFAULT_PARAMETER_2;

      public long MaxTimeAdjustment { get; set; } = DEFAULT_MAX_TIME_ADJUSTMENT;
   }
}
using MithrilShards.Core.Forge;

namespace MithrilShards.Chain.Bitcoin.Dev
{
   public static class ForgeBuilderExtensions
   {
      /// <summary>
      /// Inject the BitcoinDev shard.
      /// </summary>
      /// <param name="forgeBuilder">The forge builder.</param>
      /// <returns></returns>
      public static IForgeBuilder UseBitcoinDev(this IForgeBuilder forgeBuilder)
      {
         forgeBuilder.AddShard<BitcoinDevShard>((hostBuildContext, services) => { });

         return forgeBuilder;
      }
   }
}
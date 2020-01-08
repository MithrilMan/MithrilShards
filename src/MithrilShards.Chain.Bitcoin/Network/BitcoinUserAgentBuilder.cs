using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Network
{
   /// <summary>
   /// UserAgent is meant to be a way to identify the Forge version and shard it uses, where applicable.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Network.UserAgentBuilder" />
   public class BitcoinUserAgentBuilder : UserAgentBuilder
   {
      private readonly string bitcoinShardVersion;

      public BitcoinUserAgentBuilder(IForge forge, IForgeConnectivity forgeConnectivity) : base(forge, forgeConnectivity)
      {
         this.bitcoinShardVersion = $"BitcoinShard:{typeof(BitcoinShard).Assembly.GetName().Version.ToString(3)}";
      }

      public override string GetUserAgent()
      {
         return $"/{this.forgeVersion}/{this.bitcoinShardVersion}/";
      }
   }
}

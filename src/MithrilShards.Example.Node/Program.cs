using System.Linq;
using System.Threading.Tasks;
using MithrilShards.Core.Forge;
using MithrilShards.Diagnostic.StatisticsCollector;
using MithrilShards.Example.Network.Bedrock;
using MithrilShards.Example.Protocol;
using MithrilShards.Logging.Serilog;
using MithrilShards.Network.Bedrock;
using Serilog;

namespace MithrilShards.Example.Node
{
   static class Program
   {
      static void Main(string[] args)
      {
         bool runNode1 = args.Contains("1");
         bool runNode2 = args.Contains("2");

         // if no node 1 and node 2 are specified by args, run both in a single process
         if (runNode1 == false && runNode2 == false)
         {
            runNode1 = runNode2 = true;
         }

         Task node1 = Task.CompletedTask;
         Task node2 = Task.CompletedTask;

         if (runNode1)
         {
            node1 = new ForgeBuilder()
              .UseForge<DefaultForge>(args)
              .UseSerilog("log-settings-with-seq.json")
              .UseBedrockForgeServer<ExampleNetworkProtocolMessageSerializer>()
              .UseStatisticsCollector()
              .UseExample(KnownVersion.V1, KnownVersion.CurrentVersion)
              .RunConsoleAsync()
              ;
         }

         if (runNode2)
         {
            node2 = new ForgeBuilder()
           .UseForge<DefaultForge>(args, "forge-settings2.json")
           .UseSerilog("log-settings2.json")
           .UseBedrockForgeServer<ExampleNetworkProtocolMessageSerializer>()
           .UseStatisticsCollector()
           .UseExample(KnownVersion.V1, KnownVersion.CurrentVersion)
           .RunConsoleAsync()
           ;
         }

         Task.WaitAll(node1, node2);
      }
   }
}

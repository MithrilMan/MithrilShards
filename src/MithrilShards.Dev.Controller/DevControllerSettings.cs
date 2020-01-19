using System.Diagnostics.CodeAnalysis;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Dev.Controller
{

   public class DevControllerSettings : MithrilShardSettingsBase
   {
      /// <summary>IP address and port number on which the shard will serve its Web API endpoint.</summary>
      [DisallowNull]
      public string? EndPoint { get; set; } = "127.0.0.1:45021";
   }
}
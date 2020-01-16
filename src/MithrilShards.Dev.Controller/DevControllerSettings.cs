﻿using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Dev.Controller
{

   public class DevControllerSettings : MithrilShardSettingsBase
   {
      /// <summary>IP address and port number on which the shard will serve its WEB Api endpoint.</summary>
      public string EndPoint { get; set; }

      public DevControllerSettings()
      {

      }
   }
}
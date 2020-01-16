using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.MithrilShards;

namespace MithrilShards.Dev.Controller
{
   internal class DevControllerShard : IMithrilShard
   {
      readonly ILogger<DevControllerShard> logger;

      public DevControllerShard(ILogger<DevControllerShard> logger)
      {
         this.logger = logger;
      }

      public Task InitializeAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }
   }
}

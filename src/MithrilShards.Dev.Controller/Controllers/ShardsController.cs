using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.MithrilShards;
using System.Linq;

namespace MithrilShards.Dev.Controller.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class ShardsControllerDev : ControllerBase
   {
      readonly ILogger<PeerManagementControllerDev> _logger;
      readonly IEventBus _eventBus;
      readonly Dictionary<string, (IMithrilShard shard, IMithrilShardSettings shardSettings)> _mithrilShards;

      public ShardsControllerDev(ILogger<PeerManagementControllerDev> logger, IEventBus eventBus, IEnumerable<IMithrilShard> mithrilShards, IEnumerable<IMithrilShardSettings> mithrilShardsSettings)
      {
         _logger = logger;
         _eventBus = eventBus;
         _mithrilShards = mithrilShards.ToDictionary(shard => shard.GetType().Name, shard => (shard, mithrilShardsSettings.FirstOrDefault(settings => settings.GetType().Assembly == shard.GetType().Assembly)));
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetShards()
      {

         return Ok(
            from shardData in _mithrilShards.Values
            let shardType = shardData.shard.GetType()
            let assemblyName = shardType.Assembly.GetName()
            select new
            {
               Type = shardType.Name,
               Assembly = assemblyName.FullName,
               Version = assemblyName.Version?.ToString()
            });
      }

      [HttpGet("{shardType}")]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      public IActionResult GetShardConfiguration(string shardType)
      {
         if (_mithrilShards.TryGetValue(shardType, out (IMithrilShard shard, IMithrilShardSettings shardSettings) shardData))
         {
            if (shardData.shardSettings == null)
            {
               return Ok(new object());
            }
            else
            {
               return Ok(shardData.shardSettings);
            }
         }
         else
         {
            return NotFound();
         }
      }
   }
}

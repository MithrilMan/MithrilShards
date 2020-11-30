using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.MithrilShards;
using System.Linq;
using System;
using Microsoft.Extensions.Options;
using MithrilShards.WebApi;

namespace MithrilShards.Dev.Controller.Controllers
{
   [Area(WebApiArea.AREA_DEV)]
   public class ShardsController : MithrilControllerBase
   {
      readonly ILogger<PeerManagementController> _logger;
      readonly IEventBus _eventBus;
      readonly IServiceProvider _serviceProvider;
      readonly Dictionary<string, (IMithrilShard shard, IMithrilShardSettings shardSettings)> _mithrilShards;

      public ShardsController(ILogger<PeerManagementController> logger, IEventBus eventBus, IEnumerable<IMithrilShard> mithrilShards, IServiceProvider serviceProvider, IEnumerable<IMithrilShardSettings> mithrilShardsSettings)
      {
         _logger = logger;
         _eventBus = eventBus;
         _serviceProvider = serviceProvider;

         _mithrilShards = mithrilShards.ToDictionary(
            shard => shard.GetType().Name,
            shard => (shard, mithrilShardsSettings.FirstOrDefault(settings => settings.GetType().Assembly == shard.GetType().Assembly)));
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
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

      /// <summary>
      /// Gets the shard configuration, if exists.
      /// </summary>
      /// <remarks>
      /// A shard setting is not mandatory, so there could be shards without settings.
      /// A shard can have at maximum one settings class.
      /// There isn't a built-in mechanism to link between a setting and a shard, so by convention we consider a shard setting
      /// being a setting that lies within the same assembly of the shard.
      /// Multiple shards in the same assembly is not supported by this endpoints.
      /// </remarks>
      /// <param name="shardType">Type of the shard.</param>
      /// <returns></returns>
      [HttpGet("{shardType}")]
      [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Get))]
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
               var settings = _serviceProvider.GetService(typeof(IOptions<>).MakeGenericType(shardData.shardSettings.GetType()));
               return Ok(settings);
            }
         }
         else
         {
            return NotFound();
         }
      }
   }
}

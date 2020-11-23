using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;
using MithrilShards.Core.DevTools;
using MithrilShards.Core.EventBus;
using MithrilShards.Dev.Controller;

namespace MithrilShards.Chain.Bitcoin.Dev
{
   [ApiController]
   [DevController]
   [Route("[controller]")]
   public class BlockFetcherManagerControllerDev : ControllerBase
   {
      private readonly ILogger<ConsensusControllerDev> _logger;
      readonly IBlockFetcherManager _blockFetcherManager;

      public BlockFetcherManagerControllerDev(ILogger<ConsensusControllerDev> logger, IEventBus eventBus, IBlockFetcherManager blockFetcherManager)
      {
         _logger = logger;
         _blockFetcherManager = blockFetcherManager;
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [Route("GetInsight")]
      public ActionResult<string> GetInsight()
      {
         return Ok(((IDebugInsight)_blockFetcherManager).GetInsight());
      }
   }
}

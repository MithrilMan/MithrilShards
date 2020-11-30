using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.BlockDownloader;
using MithrilShards.Core.DevTools;
using MithrilShards.Core.EventBus;
using MithrilShards.WebApi;

namespace MithrilShards.Chain.Bitcoin.Dev
{
   [Area(WebApiArea.AREA_DEV)]
   public class BlockFetcherManagerController : MithrilControllerBase
   {
      private readonly ILogger<ConsensusController> _logger;
      readonly IBlockFetcherManager _blockFetcherManager;

      public BlockFetcherManagerController(ILogger<ConsensusController> logger, IEventBus eventBus, IBlockFetcherManager blockFetcherManager)
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

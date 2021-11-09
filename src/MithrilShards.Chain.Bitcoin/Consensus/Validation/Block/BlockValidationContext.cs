using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block;

public class BlockValidationContext : ValidationContext, IBlockValidationContext
{
   public Protocol.Types.Block Block { get; }

   public Protocol.Types.Block? KnownBlock { get; }

   /// <summary>
   /// Initializes a new instance of the <see cref="HeaderValidationContext" /> class.
   /// </summary>
   /// <param name="logger">The logger.</param>
   /// <param name="block">The block.</param>
   /// <param name="isInInitialBlockDownloadState">if set to <c>true</c> node is currently in InitialBlockDownload state.</param>
   /// <param name="chainState">State of the chain.</param>
   public BlockValidationContext(ILogger logger,
                                 Protocol.Types.Block block,
                                 bool isInInitialBlockDownloadState,
                                 IChainState chainState) : base(logger, isInInitialBlockDownloadState, chainState)
   {
      Block = block;

      KnownBlock = null; //TODO or remove this property?
   }
}

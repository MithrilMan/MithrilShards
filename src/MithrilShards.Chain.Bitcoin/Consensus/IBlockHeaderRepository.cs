using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   public interface IBlockHeaderRepository
   {
      /// <summary>
      /// Tries to add a header to the repository.
      /// </summary>
      /// <param name="header">The header to add.</param>
      /// <returns><see langword="true"/> if the header has been added, <see langword="false"/> if it was already in the repository.</returns>
      public ValueTask<bool> TryAddAsync(BlockHeader header);

      /// <summary>
      /// Gets the <see cref="BlockHeader" /> with specified hash.
      /// </summary>
      /// <param name="hash">The hash.</param>
      /// <param name="header">The header.</param>
      /// <returns><see langword="true"/> if the header has been found, <see langword="false"/> otherwise</returns>
      bool TryGet(UInt256 hash, [MaybeNullWhen(false)] out BlockHeader header);
   }
}

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block;

/// <summary>
/// Defines methods used to create an instance of a class implementing an <see cref="IBlockValidationContext"/>.
/// </summary>
public interface IBlockValidationContextFactory
{
   /// <summary>
   /// Creates a new block validation context.
   /// </summary>
   /// <param name="block">The block to be validated.</param>
   /// <param name="coinsView">The UTXO view to be used for this validation session.</param>
   /// <returns>A new block validation context.</returns>
   IBlockValidationContext Create(Protocol.Types.Block block, ICoinsView coinsView);
}

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block
{
   /// <summary>
   /// Defines methods used to create an instance of a class implementing an <see cref="IBlockValidationContext"/>.
   /// </summary>
   public interface IBlockValidationContextFactory
   {
      IBlockValidationContext Create(Protocol.Types.Block block);
   }
}
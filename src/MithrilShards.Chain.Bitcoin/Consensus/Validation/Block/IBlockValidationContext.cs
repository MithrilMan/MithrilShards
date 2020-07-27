namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Block
{
   /// <summary>
   /// Interface that exposes a block validation context during an block validation process.
   /// </summary>
   /// <seealso cref="IValidationContext" />
   public interface IBlockValidationContext : IValidationContext
   {
      /// <summary>
      /// The block to be validated.
      /// </summary>
      Protocol.Types.Block Block { get; }

      /// <summary>
      /// When this block has been already validated previously, this property returns the known block instance, null otherwise.
      /// </summary>
      /// <value>
      /// The known block that has been already validated previously.
      /// </value>
      Protocol.Types.Block? KnownBlock { get; }
   }
}

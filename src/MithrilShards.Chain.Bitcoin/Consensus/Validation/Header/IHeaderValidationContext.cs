using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;

/// <summary>
/// Interface that exposes an header validation context during an header validation process.
/// </summary>
/// <seealso cref="IValidationContext" />
public interface IHeaderValidationContext : IValidationContext
{
   /// <summary>
   /// Gets the header to be validated.
   /// </summary>
   BlockHeader Header { get; }

   /// <summary>
   /// When this header has been already validated previously, this property returns the known header node instance, null otherwise.
   /// </summary>
   /// <value>
   /// The known header that has been already validated previously.
   /// </value>
   HeaderNode? KnownHeader { get; }
}

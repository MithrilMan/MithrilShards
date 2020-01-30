using System.Collections.Generic;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation.Header
{
   /// <summary>
   /// Defines methods used to create an instance of a class implementing an <see cref="IHeaderValidationContext"/>.
   /// </summary>
   public interface IHeaderValidationContextFactory
   {
      IHeaderValidationContext Create(BlockHeader header);
   }
}
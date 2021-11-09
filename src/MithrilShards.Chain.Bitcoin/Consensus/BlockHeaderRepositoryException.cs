using System;

namespace MithrilShards.Chain.Bitcoin.Consensus;

[Serializable]
public class BlockHeaderRepositoryException : Exception
{
   public BlockHeaderRepositoryException() { }
   public BlockHeaderRepositoryException(string message) : base(message) { }
   public BlockHeaderRepositoryException(string message, Exception inner) : base(message, inner) { }
   protected BlockHeaderRepositoryException(
    System.Runtime.Serialization.SerializationInfo info,
    System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

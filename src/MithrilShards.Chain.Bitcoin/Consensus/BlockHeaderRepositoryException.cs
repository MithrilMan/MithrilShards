using System;

namespace MithrilShards.Chain.Bitcoin.Consensus;

[Serializable]
public class BlockHeaderRepositoryException : Exception
{
   public BlockHeaderRepositoryException() { }
   public BlockHeaderRepositoryException(string message) : base(message) { }
   public BlockHeaderRepositoryException(string message, Exception inner) : base(message, inner) { }
}

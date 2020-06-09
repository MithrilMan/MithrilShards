using System;
using System.Collections.Generic;
using System.Text;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// Methods to compute required work (PoW)
   /// </summary>
   public interface IProofOfWorkCalculator
   {
      uint GetNextWorkRequired(HeaderNode previousHeaderNode, BlockHeader header);
   }
}

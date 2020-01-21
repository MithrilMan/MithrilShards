using System;
using System.Collections.Generic;
using System.Text;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   [Flags]
   public enum HeaderValidity
   {
      Unknown = 0,

      /// <summary>
      /// The header has been validated
      /// </summary>
      ValidHeader = 1 << 0,

      /// <summary>
      /// All parent headers found, difficulty matches, timestamp >= median previous, checkpoint.
      /// Implies all parents are also at least <see cref="ValidTree"/>.
      /// </summary>
      ValidTree = 1 << 1,

      /// <summary>
      /// Only first tx is coinbase, 2 <= coinbase input, script length <= 100, transactions valid, no duplicate txids, sigops, size, merkle root.
      /// Implies all parents are at least TREE but not necessarily TRANSACTIONS.
      /// </summary>
      ValidTransactions = 1 << 2,

      /// <summary>
      /// Outputs do not overspend inputs, no double spends, coinbase output ok, no immature coinbase spends, BIP30.
      /// Implies all parents are also at least CHAIN.
      /// </summary>
      ValidChain = 1 << 3,

      /// <summary>
      /// Scripts and signatures ok.
      /// Implies all parents are also at least SCRIPTS.
      /// </summary>
      ValidScripts = 1 << 4,

      /// <summary>
      /// Full validation
      /// </summary>
      ValidMask = ValidHeader | ValidTree | ValidTransactions | ValidChain | ValidScripts,
   }
}

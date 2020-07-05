using System;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   /// <summary>
   /// To waste less memory, 3 different kind of enums are gropued into one, using a mask to access specific informations
   /// </summary>
   [Flags]
   public enum HeaderValidityStatuses
   {
      Unset = 0,

      /// <summary>
      /// All parent headers found, difficulty matches, timestamp >= median previous, checkpoint.
      /// Implies all parents are also at least <see cref="ValidTree"/>.
      /// </summary>
      ValidTree   /*       */ = 0b_00000000_00000001,

      /// <summary>
      /// Only first tx is coinbase, 2 <= coinbase input, script length <= 100, transactions valid, no duplicate txids, sigops, size, merkle root.
      /// Implies all parents are at least TREE but not necessarily TRANSACTIONS.
      /// </summary>
      ValidTransactions /* */ = 0b_00000000_00000010,

      /// <summary>
      /// Outputs do not overspend inputs, no double spends, coinbase output ok, no immature coinbase spends, BIP30.
      /// Implies all parents are also at least CHAIN.
      /// </summary>
      ValidChain /*        */ = 0b_00000000_00000100,

      /// <summary>
      /// Scripts and signatures ok.
      /// Implies all parents are also at least SCRIPTS.
      /// </summary>
      ValidScripts  /*     */ = 0b_00000000_00001000,

      /// <summary>Full validation mask</summary>
      ValidMask /*         */ = 0b_00000000_00001111,


      /// <summary>
      /// Validation failed at the stage after last raised valid flag.
      /// </summary>
      Failed /*            */ = 0b_00000000_10000000,

      /// <summary>
      /// Descends from a failed block.
      /// </summary>
      FailedChild /*       */ = 0b_00000000_01000000,

      /// <summary>Failure mask.</summary>
      FailedMask /*        */ = 0b_11000000_11000000
   }
}

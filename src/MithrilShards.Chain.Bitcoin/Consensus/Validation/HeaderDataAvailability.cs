using System;

namespace MithrilShards.Chain.Bitcoin.Consensus.Validation
{
   /// <summary>
   /// Enum the reflects the data availability of a block.
   /// </summary>
   [Flags]
   public enum HeaderDataAvailability
   {
      /// <summary>
      /// Full block available.
      /// </summary>
      HasBlockData /*      */ = 0b_00000001_00000000,

      /// <summary>
      /// Undo data available.
      /// </summary>
      HasUndoData /*       */ = 0b_00000010_00000000,

      /// <summary>
      /// Block data was received with a witness-enforcing client.
      /// </summary>
      OptWitness /*        */ = 0b_00000100_00000000,

      /// <summary>The availability mask</summary>
      AvailabilityMask /*  */ = 0b_00000111_00000000,
   }
}

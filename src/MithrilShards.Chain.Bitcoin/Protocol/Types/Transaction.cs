using System;

namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// transaction (tx).
   /// </summary>
   public class Transaction
   {
      /// <summary>
      /// Block version information (note, this is signed)
      /// </summary>
      public int Version { get; set; }

      ///// <summary>
      ///// Used to communicate which kind of content the transaction exposes.
      ///// Actually it's not always present.
      ///// If present, always 0001, and indicates the presence of witness data
      ///// </summary>
      //public byte Flags { get; set; }

      /// <summary>
      /// A list of 1 or more transaction inputs or sources for coins.
      /// </summary>
      public TransactionInput[]? Inputs { get; set; }

      /// <summary>
      /// A list of 1 or more transaction outputs or destinations for coins.
      /// </summary>
      public TransactionOutput[]? Outputs { get; set; }

      ///// <summary>
      ///// A list of witnesses, one for each input.
      ///// Omitted if the transaction doesn't contains witness data (see <see cref="Flags"/>).
      ///// </summary>
      //public TransactionWitness[]? Witnesses { get; set; }

      /// <summary>
      /// The block number or timestamp at which this transaction is unlocked:
      /// 0             = Not locked.
      /// < 500000000   = Block number at which this transaction is unlocked.
      /// >= 500000000  = UNIX timestamp at which this transaction is unlocked.
      ///
      /// If all TxIn inputs have final (0xffffffff) sequence numbers then LockTime is irrelevant.
      /// Otherwise, the transaction may not be added to a block until after LockTime
      /// </summary>
      public uint LockTime { get; set; }

      public bool HasWitness()
      {
         if (Inputs == null) return false;

         for (int i = 0; i < Inputs!.Length; i++)
         {
            if ((Inputs[i].ScriptWitness?.Components?.Length ?? 0) > 0)
            {
               return true;
            }
         }
         return false;
      }
   }
}
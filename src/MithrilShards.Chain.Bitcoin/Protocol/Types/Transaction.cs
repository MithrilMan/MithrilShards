using MithrilShards.Core.DataTypes;

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

      /// <summary>
      /// A list of 1 or more transaction inputs or sources for coins.
      /// </summary>
      public TransactionInput[]? Inputs { get; set; }

      /// <summary>
      /// A list of 1 or more transaction outputs or destinations for coins.
      /// </summary>
      public TransactionOutput[]? Outputs { get; set; }

      /// <summary>
      /// The block number or timestamp at which this transaction is unlocked:
      /// 0             = Not locked.
      /// &lt;= 500000000  = Block number at which this transaction is unlocked.
      /// &gt;= 500000000  = UNIX timestamp at which this transaction is unlocked.
      ///
      /// If all TxIn inputs have final (0xffffffff) sequence numbers then LockTime is irrelevant.
      /// Otherwise, the transaction may not be added to a block until after LockTime
      /// </summary>
      public uint LockTime { get; set; }

      /// <summary>
      /// Not part of the protocol message, this property represents the transaction hash.
      /// Not used during serialization, it's computed externally when received from other peers (or during a block generation like during mining).
      /// </summary>
      /// <remarks>
      /// This property is set when a block is received from a peer (or when it's mined).
      /// It's not part of the protocol message and doesn't participate to serialization.
      /// </remarks>
      public UInt256? Hash { get; set; }

      /// <summary>
      /// Not part of the protocol message, this property represents the witness transaction hash.
      /// Not used during serialization, it's computed externally when received from other peers (or during a block generation like during mining).
      /// </summary>
      /// <remarks>
      /// This property is set when a block is received from a peer (or when it's mined).
      /// It's not part of the protocol message and doesn't participate to serialization.
      /// </remarks>
      public UInt256? WitnessHash { get; set; }




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

      /// <summary>
      /// Determines whether this transaction represents a coinbase transaction.
      /// </summary>
      /// <returns>
      ///   <c>true</c> if [is coin base]; otherwise, <c>false</c>.
      /// </returns>
      public bool IsCoinBase()
      {
         return Inputs!.Length == 1
             && Inputs[0].PreviousOutput!.IsNull()
             && Outputs!.Length >= 1;
      }
   }
}
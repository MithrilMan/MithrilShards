namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// transaction input (tx_in).
   /// </summary>
   public class TransactionInput
   {
      /// <summary>
      /// The previous output transaction reference
      /// </summary>
      public OutPoint? PreviousOutput { get; set; }

      /// <summary>
      /// Used to communicate which kind of content the transaction exposes.
      /// Actually it's not always present.
      /// If present, always 0001, and indicates the presence of witness data
      /// </summary>
      public byte[]? SignatureScript { get; set; }

      /// <summary>
      /// Transaction version as defined by the sender.
      /// Intended for "replacement" of transactions when information is updated before inclusion into a block.
      /// </summary>
      public uint Sequence { get; set; }

      /// <summary>
      /// The script witness relative to the input.
      /// May be null when witness is not enabled or the data received doesn't contain witness data.
      /// </summary>
      public TransactionWitness? ScriptWitness { get; set; }
   }
}
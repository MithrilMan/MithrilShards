namespace MithrilShards.Chain.Bitcoin.Protocol.Types;

/// <summary>
/// Represents a Transaction Input.
/// Transaction Inputs are unspent output used to fund a transaction.
/// </summary>
/// <remarks>BTC-TER: tx_in</remarks>
public class TransactionInput
{
   /// <summary>
   /// The previous output transaction reference.
   /// </summary>
   /// <remarks>In bitcoin core terminology this is called PrevOut.</remarks>
   public OutPoint? PreviousOutput { get; set; }

   /// <summary>
   /// The unlocking script that satisfies the conditions placed on the output by the PubKeyScript and is what allows the output to be spent.
   /// </summary>
   /// <remarks>BTC-TER: scriptSig.</remarks>
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

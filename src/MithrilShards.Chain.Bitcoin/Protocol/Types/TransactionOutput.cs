namespace MithrilShards.Chain.Bitcoin.Protocol.Types;

/// <summary>
/// transaction output (tx_out).
/// </summary>
public class TransactionOutput
{
   /// <summary>
   /// Transaction Value
   /// </summary>
   public long Value { get; set; }

   /// <summary>
   /// The script used to claim this output.
   /// Usually contains the public key as a Bitcoin script setting up conditions to claim this output.
   /// </summary>
   public byte[]? PublicKeyScript { get; set; }
}

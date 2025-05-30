using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus;

public interface ICoinsView
{
   /// <summary>
   /// Tries to get the unspent transaction output (UTXO) for a given outpoint.
   /// </summary>
   /// <param name="outPoint">The outpoint to look up.</param>
   /// <param name="output">The transaction output, if found and unspent.</param>
   /// <returns><c>true</c> if the UTXO is found and unspent, <c>false</c> otherwise.</returns>
   bool TryGetOutput(OutPoint outPoint, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out TransactionOutput output);

   /// <summary>
   /// Gets the unspent transaction output (UTXO) for a given outpoint.
   /// Returns null if not found or spent.
   /// </summary>
   /// <param name="outPoint">The outpoint to look up.</param>
   /// <returns>The <see cref="TransactionOutput"/> or <c>null</c> if not found/spent.</returns>
   TransactionOutput? GetOutput(OutPoint outPoint);

   // Potentially other methods like:
   // bool HaveInput(OutPoint outPoint);
   // UInt256 GetBestBlockHash();
   // bool BatchWrite(IEnumerable<KeyValuePair<OutPoint, TransactionOutput?>> changes); // For applying/reverting blocks
}

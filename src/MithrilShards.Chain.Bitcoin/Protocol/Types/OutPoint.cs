using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   /// <summary>
   /// Point to a specific transaction output.
   /// </summary>
   public class OutPoint
   {
      /// <summary>
      /// The hash of the referenced transaction.
      /// </summary>
      public UInt256 Hash { get; set; } = UInt256.Zero;

      /// <summary>
      /// The index of the specific output in the transaction. The first output is 0, etc.
      /// </summary>
      public uint Index { get; set; } = uint.MaxValue;

      /// <summary>
      /// Determines whether this instance is null.
      /// </summary>
      /// <returns>
      ///   <c>true</c> if this instance is null; otherwise, <c>false</c>.
      /// </returns>
      public bool IsNull()
      {
         return Hash == UInt256.Zero && Index == uint.MaxValue;
      }
   }
}

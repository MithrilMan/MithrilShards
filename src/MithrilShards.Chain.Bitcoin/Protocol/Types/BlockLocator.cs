using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol.Types
{
   public class BlockLocator
   {
      /// <summary>
      /// Block locator objects.
      /// Newest back to genesis block (dense to start, but then sparse)
      /// </summary>
      public UInt256[] BlockLocatorHashes { get; set; }
   }
}


namespace MithrilShards.Core.Network {
   /// <summary>
   /// The version peers agrees to use when their respective version doesn't match.
   /// It should be the lower common version both parties implements.
   /// </summary>
   public class NegotiatedProtocolVersion : INegotiatedProtocolVersion {
      public int Version { get; set; } = 0;
   }
}

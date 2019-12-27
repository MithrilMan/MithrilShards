
namespace MithrilShards.Core.Network {
   /// <summary>
   /// The version peers agrees to use when their respective version doesn't match.
   /// It should be the lower common version both parties implements.
   /// </summary>
   public interface INegotiatedProtocolVersion {
      /// <summary>
      /// Gets the version peers agrees to use when their respective version doesn't match.
      /// It should be the lower common version both parties implements.
      /// </summary>
      int Version { get; set; }
   }
}

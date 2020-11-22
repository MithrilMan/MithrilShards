namespace MithrilShards.Example.Network
{
   /// <summary>
   /// Holds information about current implemented version and minimum required version from other peers.
   /// </summary>
   public class NodeImplementation
   {
      /// <summary>
      /// Gets the minimum version local node supports.
      /// </summary>
      /// <value>
      /// The minimum version local node wants to support.
      /// </value>
      public int MinimumSupportedVersion { get; }

      /// <summary>
      /// Gets the version message our peer try to negotiate to when connecting to other peers.
      /// </summary>
      /// <value>
      /// The implementation version we want to use.
      /// </value>
      public int ImplementationVersion { get; }

      public NodeImplementation(int minimumSupportedVersion, int implementationVersion)
      {
         MinimumSupportedVersion = minimumSupportedVersion;
         ImplementationVersion = implementationVersion;
      }
   }
}

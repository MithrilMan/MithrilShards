using MithrilShards.Chain.Bitcoin.Protocol.Messages;

namespace MithrilShards.Chain.Bitcoin.Network {
   /// <summary>
   /// Holds information about current implemented version.
   /// </summary>
   public class AnnouncingVersion {
      /// <summary>
      /// Gets the version message to use during handshake to communicate other peers our version.
      /// </summary>
      /// <value>
      /// The implementation version.
      /// </value>
      public VersionMessage ImplementationVersion { get; }

      public AnnouncingVersion(VersionMessage implementationVersion) {
         this.ImplementationVersion = implementationVersion;
      }
   }
}

using MithrilShards.Core.DataTypes;

namespace MithrilShards.Core.Network.Protocol
{
   public interface IChainDefinition
   {
      /// <summary>
      /// Gets the name of the chain definition (e.g. "Bitcoin Main")
      /// </summary>
      string Name { get; }

      /// <summary>
      /// The magic value used to identify the beginning of a message in a stream of received bytes from other peers.
      /// </summary>
      uint Magic { get; }

      /// <summary>
      /// Byte representation of <see cref="Magic"/>.
      /// </summary>
      byte[] MagicBytes { get; }

      /// <summary>
      /// Gets the hash of the first block (aka genesis) of the chain.
      /// </summary>
      UInt256 Genesis { get; }

      /// <summary>
      /// Gets the maximum allowed size of a message payload (in bytes).
      /// If a node sends a bigger payload, the payload is rejected and the client should be banned.
      /// This is considered a default value but some implementation may override this value.
      /// </summary>
      int DefaultMaxPayloadSize { get; }

      /// <summary>
      /// Gets or sets the pow target spacing.
      /// </summary>
      /// <value>
      /// The pow target spacing.
      /// </value>
      long PowTargetSpacing { get; }
   }
}

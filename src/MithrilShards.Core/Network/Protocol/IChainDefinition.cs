namespace MithrilShards.Core.Network.Protocol {
   public interface IChainDefinition {
      string Name { get; }

      uint Magic { get; }

      byte[] MagicBytes { get; }

      byte[] genesis { get; }
   }
}

namespace MithrilShards.Core.Network.Protocol
{
   /// <summary>
   /// Interfaces that define a generic network message
   /// </summary>
   public interface INetworkMessage
   {
      string Command { get; }
   }
}

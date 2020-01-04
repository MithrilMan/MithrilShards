namespace MithrilShards.Core.Network.Server.Guards
{
   public interface IServerPeerConnectionGuard
   {
      ServerPeerConnectionGuardResult Check(IPeerContext peerEndPoint);
   }
}

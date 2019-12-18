namespace MithrilShards.P2P.Network.StateMachine {
   /// <summary>
   /// State of the peer connection.
   /// </summary>
   public enum PeerConnectionTrigger : int {
      Connect,
      Cancel,
      ConnectionFail,
      Connected,
      AcceptConnection,
      Disconnect
   }
}

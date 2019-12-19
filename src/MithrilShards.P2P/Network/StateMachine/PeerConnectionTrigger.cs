namespace MithrilShards.P2P.Network.StateMachine {
   /// <summary>
   /// State of the peer connection.
   /// </summary>
   public enum PeerConnectionTrigger : int {
      AcceptConnection,
      Connect,
      Cancel,
      ConnectionFail,
      Connected,
      Disconnect,
      PeerDisconnected,
      DisconnectFromPeer,
      WaitPeerStartsHandShake,
      VersionMessageReceived,
      PeerStartedHandshake,
      ProcessMessage,
      WaitMessage,
      MessageProcessed
   }
}

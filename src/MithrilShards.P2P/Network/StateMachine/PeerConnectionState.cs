namespace MithrilShards.P2P.Network.StateMachine {
   /// <summary>
   /// State of the peer connection.
   /// </summary>
   public enum PeerConnectionState : int {
      ///// <summary>Initial state of an outbound peer.</summary>
      //Initializing = 0,

      ///// <summary>Connection to another peer has been established.</summary>
      //Connected,

      ///// <summary>The peer is performing handshaking with the other peer.</summary>
      //HandShaking,

      ///// <summary>The node and the peer exchanged version information.</summary>
      //HandShaked,

      ///// <summary>The peer is performing disconnection against the other peer.</summary>
      //Disconnecting,

      ///// <summary>The peer is disconnected.</summary>
      //Disconnected,

      ///// <summary>An error occurred.</summary>
      //Failure,
      //Cancelled
      Connected,
      Initializing,
      Connecting,
      Cancelled,
      ConnectionFailed
   }
}

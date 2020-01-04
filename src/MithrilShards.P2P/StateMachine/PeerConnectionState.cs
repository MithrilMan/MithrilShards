﻿namespace MithrilShards.Network.Legacy.StateMachine
{
   /// <summary>
   /// State of the peer connection.
   /// </summary>
   public enum PeerConnectionState : int
   {
      Initializing,
      Disconnectable,
      Connected,
      WaitingMessage,
      ProcessMessage,
      Disconnecting,
      Disconnected,

      //Connecting,
      //Cancelled,
      //ConnectionFailed,
      //WaitingPeerInitiateHandShake,
      //PeerStartedHandshake,
      //HandlingMessage,
   }
}

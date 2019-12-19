using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.P2P.Network.Events;
using NBitcoin.Protocol;
using Stateless;

namespace MithrilShards.P2P.Network.StateMachine {
   public class PeerConnectionStateMachine {
      readonly StateMachine<PeerConnectionState, PeerConnectionTrigger> stateMachine;
      readonly ILogger logger;
      readonly IEventBus eventBus;
      readonly PeerConnection peerConnection;
      readonly PeerConnectionDirection peerDirection;

      readonly IPEndPoint remoteEndPoint;

      private PipeWriter pipeWriter;
      private PipeReader pipeReader;

      /// <summary>
      /// Triggers the message processing passing the message that has to be processed.
      /// </summary>
      private readonly StateMachine<PeerConnectionState, PeerConnectionTrigger>.TriggerWithParameters<Message> processMessageTrigger;

      /// <summary>
      /// Triggers a disconnection caused by our server, specifying the reason.
      /// </summary>
      private readonly StateMachine<PeerConnectionState, PeerConnectionTrigger>.TriggerWithParameters<(string reason, Exception ex)> disconnectFromPeerTrigger;

      /// <summary>
      /// Triggers a disconnection caused by the other peer, specifying the reason.
      /// </summary>
      private readonly StateMachine<PeerConnectionState, PeerConnectionTrigger>.TriggerWithParameters<(string reason, Exception ex)> peerDroppedTrigger;

      public PeerConnectionState Status { get => this.stateMachine.State; }


      public PeerConnectionStateMachine(ILogger logger, IEventBus eventBus, PeerConnection peerConnection, CancellationToken cancellationToken) {
         this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
         this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
         this.peerConnection = peerConnection ?? throw new ArgumentNullException(nameof(peerConnection));
         this.stateMachine = new StateMachine<PeerConnectionState, PeerConnectionTrigger>(PeerConnectionState.Initializing, FiringMode.Immediate);

         this.remoteEndPoint = this.peerConnection.ConnectedClient.Client.RemoteEndPoint as IPEndPoint;

         this.processMessageTrigger = this.stateMachine.SetTriggerParameters<Message>(PeerConnectionTrigger.ProcessMessage);
         this.disconnectFromPeerTrigger = this.stateMachine.SetTriggerParameters<(string reason, Exception ex)>(PeerConnectionTrigger.DisconnectFromPeer);
         this.peerDroppedTrigger = this.stateMachine.SetTriggerParameters<(string reason, Exception ex)>(PeerConnectionTrigger.PeerDropped);


         if (this.peerConnection.Direction == PeerConnectionDirection.Inbound) {
            this.ConfigureInboundStateMachine(cancellationToken);
         }
         else {
            this.ConfigureOutboundStateMachine(cancellationToken);
         }

         //this.stateMachine.OnUnhandledTrigger((state, trigger) => {
         //   this.logger.LogError("Unhandled Trigger: '{State}' state, '{trigger}' trigger!", state, trigger);
         //});

         this.EnableStateTransitionLogs();
      }

      internal void ForceDisconnection() {
         if (this.stateMachine.IsInState(PeerConnectionState.Connected)) {
            this.stateMachine.FireAsync(this.disconnectFromPeerTrigger, (reason: "Unexpected state transition.", ex: (Exception)null));
         }
      }

      private void ConfigureInboundStateMachine(CancellationToken cancellationToken) {
         using (this.logger.BeginScope("Inbound Peer State Machine")) {
            this.stateMachine.Configure(PeerConnectionState.Initializing)
               .Permit(PeerConnectionTrigger.AcceptConnection, PeerConnectionState.Connected);

            this.stateMachine.Configure(PeerConnectionState.Disconnectable)
               .Permit(PeerConnectionTrigger.DisconnectFromPeer, PeerConnectionState.Disconnecting)
               .Permit(PeerConnectionTrigger.PeerDropped, PeerConnectionState.Disconnecting);

            this.stateMachine.Configure(PeerConnectionState.Connected)
               .SubstateOf(PeerConnectionState.Disconnectable)
               .OnEntryAsync(async () => await this.StartReceivingMessages(cancellationToken).ConfigureAwait(false))
               .Permit(PeerConnectionTrigger.WaitMessage, PeerConnectionState.WaitingMessage);

            this.stateMachine.Configure(PeerConnectionState.WaitingMessage)
               .SubstateOf(PeerConnectionState.Connected)
               .Permit(PeerConnectionTrigger.ProcessMessage, PeerConnectionState.ProcessMessage);

            this.stateMachine.Configure(PeerConnectionState.ProcessMessage)
               .SubstateOf(PeerConnectionState.Connected)
               .OnEntryFromAsync(this.processMessageTrigger,
                                 async (message) => await this.ProgessMessageAsync(message, cancellationToken).ConfigureAwait(false))
               .Permit(PeerConnectionTrigger.MessageProcessed, PeerConnectionState.WaitingMessage);

            this.stateMachine.Configure(PeerConnectionState.Disconnecting)
               .OnEntryFromAsync(this.peerDroppedTrigger,
                                 async (why) => await this.DisconnectingAsync(why.reason, why.ex, cancellationToken).ConfigureAwait(false))
               .OnEntryFromAsync(this.disconnectFromPeerTrigger,
                                 async (why) => await this.DisconnectingAsync(why.reason, why.ex, cancellationToken).ConfigureAwait(false))
               .Permit(PeerConnectionTrigger.PeerDisconnected, PeerConnectionState.Disconnected);

            this.stateMachine.Configure(PeerConnectionState.Disconnected)
               .OnEntry(this.Disconnected);

            string graph = Stateless.Graph.UmlDotGraph.Format(this.stateMachine.GetInfo());
         }
      }

      private void ConfigureOutboundStateMachine(CancellationToken cancellationToken) {

      }

      public async Task AcceptIncomingConnection() {
         await this.stateMachine.FireAsync(PeerConnectionTrigger.AcceptConnection).ConfigureAwait(false);
      }

      /// <summary>
      /// Reads messages from the connection stream.
      /// </summary>
      private async Task StartReceivingMessages(CancellationToken cancellationToken) {
         var pipe = new Pipe();
         this.pipeWriter = pipe.Writer;
         this.pipeReader = pipe.Reader;

         try {
            using (NetworkStream stream = this.peerConnection.ConnectedClient.GetStream()) {
               await this.stateMachine.FireAsync(PeerConnectionTrigger.WaitMessage).ConfigureAwait(false);

               while (!cancellationToken.IsCancellationRequested) {
                  //TODO usare System.IO.Pipelines
                  // reading data from a pipe instance
                  //ReadResult result = await this.pipeReader.ReadAsync(this.cancellationToken);
                  //ReadOnlySequence<byte> buffer = result.Buffer;
                  //SequencePosition? position = null;
                  //this.connectedClient.GetStream();
                  //// We perform calculations with the data obtained.
                  //await _bytesProcessor.ProcessBytesAsync(buffer, token);

                  Message readMessage = null;
                  await Task.Run(() => {
                     readMessage = Message.ReadNext(this.peerConnection.ConnectedClient.GetStream(), NBitcoin.Network.Main, 70015, cancellationToken, out _);
                  }).ConfigureAwait(false);
               }
            }
         }
         catch (Exception ex) when (ex is IOException || ex is OperationCanceledException || ex is ObjectDisposedException) {
            await this.stateMachine.FireAsync(this.peerDroppedTrigger,
                                              (reason: "The node stopped receiving messages.", ex)).ConfigureAwait(false);
            return;
         }
         catch (Exception ex) {
            await this.stateMachine.FireAsync(this.peerDroppedTrigger,
                                              (reason: "Unexpected failure whilst receiving messages.", ex)).ConfigureAwait(false);
            return;
         }
      }


      private Task ProgessMessageAsync(Message message, CancellationToken cancellationToken) {
         throw new NotImplementedException();
      }

      private void Disconnected() {
         this.logger.LogDebug("Peer {PeerConnectionId} Disconnected", this.peerConnection.PeerConnectionId);
      }

      private Task DisconnectingAsync(string reason, Exception ex, CancellationToken cancellationToken) {
         this.logger.LogDebug(ex, "Disconnecting {PeerConnectionId}: {Reason}", this.peerConnection.PeerConnectionId, reason);
         this.peerConnection.ConnectedClient.Close();
         this.eventBus.Publish(new Events.PeerDisconnected(this.peerConnection.Direction, this.peerConnection.PeerConnectionId, this.remoteEndPoint, reason, ex));
         this.stateMachine.Fire(PeerConnectionTrigger.PeerDisconnected);
         return Task.CompletedTask;
      }


      /// <summary>
      /// Enables the state transition logs.
      /// </summary>
      /// <remarks>Transition logs are enabled only if the code is compiled with DEBUG symbol</remarks>
      [Conditional("DEBUG")]
      private void EnableStateTransitionLogs() {
         this.stateMachine.OnTransitioned(transition => {
            this.logger.LogDebug("From {FromState} to {ToState}", transition.Source, transition.Destination);
         });
      }
   }
}
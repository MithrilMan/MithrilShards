using System;
using System.Buffers;
using System.Collections.Generic;
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
      private readonly Guid machineId;
      readonly ILogger logger;
      readonly IEventBus eventBus;
      readonly PeerConnectionDirection peerConnectionDirection;
      readonly TcpClient connectedClient;

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
      private readonly StateMachine<PeerConnectionState, PeerConnectionTrigger>.TriggerWithParameters<(string reason, Exception ex)> peerDisconnectedTrigger;

      public PeerConnectionState Status { get => this.stateMachine.State; }


      public PeerConnectionStateMachine(ILogger logger, IEventBus eventBus, PeerConnectionDirection peerConnectionDirection, TcpClient connectedClient, CancellationToken cancellationToken) {
         this.logger = logger;
         this.eventBus = eventBus;
         this.peerConnectionDirection = peerConnectionDirection;
         this.connectedClient = connectedClient;
         this.stateMachine = new StateMachine<PeerConnectionState, PeerConnectionTrigger>(PeerConnectionState.Initializing);

         this.machineId = Guid.NewGuid();

         this.remoteEndPoint = this.connectedClient.Client.RemoteEndPoint as IPEndPoint;

         this.processMessageTrigger = this.stateMachine.SetTriggerParameters<Message>(PeerConnectionTrigger.ProcessMessage);
         this.disconnectFromPeerTrigger = this.stateMachine.SetTriggerParameters<(string reason, Exception ex)>(PeerConnectionTrigger.DisconnectFromPeer);
         this.peerDisconnectedTrigger = this.stateMachine.SetTriggerParameters<(string reason, Exception ex)>(PeerConnectionTrigger.PeerDisconnected);


         if (peerConnectionDirection == PeerConnectionDirection.Inbound) {
            this.ConfigureInboundStateMachine(cancellationToken);
         }
         else {
            this.ConfigureOutboundStateMachine(cancellationToken);
         }
      }

      private void ConfigureInboundStateMachine(CancellationToken cancellationToken) {
         using (this.logger.BeginScope("Inbound Peer State Machine")) {
            this.stateMachine.Configure(PeerConnectionState.Initializing)
               .Permit(PeerConnectionTrigger.AcceptConnection, PeerConnectionState.Connected);

            this.stateMachine.Configure(PeerConnectionState.Disconnectable)
               .Permit(PeerConnectionTrigger.DisconnectFromPeer, PeerConnectionState.Disconnecting)
               .Permit(PeerConnectionTrigger.PeerDisconnected, PeerConnectionState.Disconnecting);

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
               .OnEntryFromAsync(this.peerDisconnectedTrigger,
                                 async (why) => await this.DisconnectAsync(why.reason, why.ex, cancellationToken).ConfigureAwait(false))
               .OnEntryFromAsync(this.disconnectFromPeerTrigger,
                                 async (why) => await this.DisconnectAsync(why.reason, why.ex, cancellationToken).ConfigureAwait(false))
               .Permit(PeerConnectionTrigger.PeerDisconnected, PeerConnectionState.Disconnected);

            this.stateMachine.Configure(PeerConnectionState.Disconnected)
               .OnEntry(this.CleanUp);

            this.stateMachine.OnUnhandledTrigger((state, trigger) => {
               this.logger.LogError("Unhandled Trigger: '{State}' state, '{trigger}' trigger!", state, trigger);
            });
         }
      }

      private void ConfigureOutboundStateMachine(CancellationToken cancellationToken) {


         this.stateMachine.OnUnhandledTrigger((state, trigger) => {
            this.logger.LogError("Unhandled Trigger: '{State}' state, '{trigger}' trigger!", state, trigger);
         });
      }

      public async Task AcceptOutgoingConnection() {
         await this.stateMachine.FireAsync(PeerConnectionTrigger.Connect).ConfigureAwait(false);
      }

      /// <summary>
      /// Reads messages from the connection stream.
      /// </summary>
      private async Task StartReceivingMessages(CancellationToken cancellationToken) {
         var pipe = new Pipe();
         this.pipeWriter = pipe.Writer;
         this.pipeReader = pipe.Reader;

         try {
            while (!cancellationToken.IsCancellationRequested) {
               //TODO usare System.IO.Pipelines
               // reading data from a pipe instance
               //ReadResult result = await this.pipeReader.ReadAsync(this.cancellationToken);
               //ReadOnlySequence<byte> buffer = result.Buffer;
               //SequencePosition? position = null;
               //this.connectedClient.GetStream();
               //// We perform calculations with the data obtained.
               //await _bytesProcessor.ProcessBytesAsync(buffer, token);



            }
         }
         catch (Exception ex) when (ex is IOException || ex is OperationCanceledException || ex is ObjectDisposedException) {
            await this.stateMachine.FireAsync(this.peerDisconnectedTrigger,
                                              (reason: "The node stopped receiving messages.", ex)).ConfigureAwait(false);
         }
         catch (Exception ex) {
            await this.stateMachine.FireAsync(this.peerDisconnectedTrigger,
                                              (reason: "Unexpected failure whilst receiving messages.", ex)).ConfigureAwait(false);
         }
      }


      private Task ProgessMessageAsync(Message message, CancellationToken cancellationToken) {
         throw new NotImplementedException();
      }

      private void CleanUp() {
         throw new NotImplementedException();
      }

      private Task DisconnectAsync(string reason, Exception ex, CancellationToken cancellationToken) {
         throw new NotImplementedException();
      }
   }
}

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using Stateless;

namespace MithrilShards.Network.Legacy.StateMachine
{
   public class PeerConnectionStateMachine
   {
      readonly StateMachine<PeerConnectionState, PeerConnectionTrigger> stateMachine;
      readonly ILogger logger;
      readonly IEventBus eventBus;
      readonly PeerConnection peerConnection;
      readonly NetworkMessageDecoder networkMessageDecoder;
      readonly PeerConnectionDirection peerDirection;
      private SequencePosition examined;
      private SequencePosition consumed;

      /// <summary>
      /// Triggers the message processing passing the message that has to be processed.
      /// </summary>
      private readonly StateMachine<PeerConnectionState, PeerConnectionTrigger>.TriggerWithParameters<INetworkMessage> processMessageTrigger;

      /// <summary>
      /// Triggers a disconnection caused by our server, specifying the reason.
      /// </summary>
      private readonly StateMachine<PeerConnectionState, PeerConnectionTrigger>.TriggerWithParameters<(string reason, Exception ex)> disconnectFromPeerTrigger;

      /// <summary>
      /// Triggers a disconnection caused by the other peer, specifying the reason.
      /// </summary>
      private readonly StateMachine<PeerConnectionState, PeerConnectionTrigger>.TriggerWithParameters<(string reason, Exception ex)> peerDroppedTrigger;

      public PeerConnectionState Status { get => this.stateMachine.State; }


      public PeerConnectionStateMachine(ILogger logger, IEventBus eventBus, PeerConnection peerConnection, NetworkMessageDecoder networkMessageDecoder, CancellationToken cancellationToken)
      {
         this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
         this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
         this.peerConnection = peerConnection ?? throw new ArgumentNullException(nameof(peerConnection));
         this.networkMessageDecoder = networkMessageDecoder;
         this.stateMachine = new StateMachine<PeerConnectionState, PeerConnectionTrigger>(PeerConnectionState.Initializing, FiringMode.Immediate);

         this.processMessageTrigger = this.stateMachine.SetTriggerParameters<INetworkMessage>(PeerConnectionTrigger.ProcessMessage);
         this.disconnectFromPeerTrigger = this.stateMachine.SetTriggerParameters<(string reason, Exception ex)>(PeerConnectionTrigger.DisconnectFromPeer);
         this.peerDroppedTrigger = this.stateMachine.SetTriggerParameters<(string reason, Exception ex)>(PeerConnectionTrigger.PeerDropped);

         if (this.peerConnection.Direction == PeerConnectionDirection.Inbound)
         {
            this.ConfigureInboundStateMachine(cancellationToken);
         }
         else
         {
            this.ConfigureOutboundStateMachine(cancellationToken);
         }

         this.EnableStateTransitionLogs();
      }

      internal void ForceDisconnection()
      {
         if (this.stateMachine.IsInState(PeerConnectionState.Connected))
         {
            this.stateMachine.FireAsync(this.disconnectFromPeerTrigger, (reason: "Unexpected state transition.", ex: (Exception)null));
         }
      }

      private void ConfigureInboundStateMachine(CancellationToken cancellationToken)
      {
         using (this.logger.BeginScope("Inbound Peer State Machine"))
         {
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
                                 async (message) => await this.ProcessMessageAsync(message, cancellationToken).ConfigureAwait(false))
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

      private void ConfigureOutboundStateMachine(CancellationToken cancellationToken)
      {

      }

      public async Task AcceptIncomingConnection()
      {
         await this.stateMachine.FireAsync(PeerConnectionTrigger.AcceptConnection).ConfigureAwait(false);
      }

      /// <summary>
      /// Reads messages from the connection stream.
      /// </summary>
      private async Task StartReceivingMessages(CancellationToken cancellationToken)
      {
         try
         {
            await this.stateMachine.FireAsync(PeerConnectionTrigger.WaitMessage).ConfigureAwait(false);

            await this.ProcessNetworkMessages(this.peerConnection.ConnectedClient.Client, cancellationToken).ConfigureAwait(false);
         }
         catch (Exception ex) when (ex is IOException || ex is OperationCanceledException || ex is ObjectDisposedException)
         {
            await this.stateMachine.FireAsync(this.peerDroppedTrigger,
                                              (reason: "The node stopped receiving messages.", ex)).ConfigureAwait(false);
            return;
         }
         catch (Exception ex)
         {
            await this.stateMachine.FireAsync(this.peerDroppedTrigger,
                                              (reason: "Unexpected failure whilst receiving messages.", ex)).ConfigureAwait(false);
            return;
         }
         finally
         {
            //TODO: close pipes
         }
      }

      private async Task ProcessNetworkMessages(Socket socket, CancellationToken cancellationToken)
      {
         var pipe = new Pipe();

         Task writer = this.FillPipeAsync(socket, pipe.Writer, cancellationToken);
         Task reader = this.ProcessMessagesAsync(pipe.Reader, cancellationToken);

         await Task.WhenAll(writer, reader).ConfigureAwait(false);

         await this.stateMachine.FireAsync(this.disconnectFromPeerTrigger, (reason: "Peer Disconnected", ex: (Exception)null)).ConfigureAwait(false);
      }


      private async Task FillPipeAsync(Socket socket, PipeWriter writer, CancellationToken cancellationToken)
      {
         const int minimumBufferSize = 512;

         while (!cancellationToken.IsCancellationRequested)
         {
            // Allocate at least 512 bytes from the PipeWriter.
            Memory<byte> memory = writer.GetMemory(minimumBufferSize);
            int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None).ConfigureAwait(false);
            if (bytesRead == 0)
            {
               break;
            }
            // Tell the PipeWriter how much was read from the Socket.
            writer.Advance(bytesRead);

            // Make the data available to the PipeReader.
            FlushResult result = await writer.FlushAsync().ConfigureAwait(false);

            if (result.IsCompleted)
            {
               break;
            }
         }

         // By completing PipeWriter, tell the PipeReader that there's no more data coming.
         await writer.CompleteAsync().ConfigureAwait(false);
      }

      async Task ProcessMessagesAsync(PipeReader reader, CancellationToken cancellationToken = default)
      {
         try
         {
            while (true)
            {
               ReadResult result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
               ReadOnlySequence<byte> buffer = result.Buffer;

               try
               {
                  // Process one message from the buffer, modifying the input buffer on each iteration.
                  if (this.networkMessageDecoder.TryParseMessage(in buffer, out this.consumed, out this.examined, out INetworkMessage message))
                  {
                     await this.stateMachine.FireAsync(this.processMessageTrigger, message).ConfigureAwait(false);
                  }

                  // There's no more data to be processed.
                  if (result.IsCompleted)
                  {
                     if (buffer.Length > 0)
                     {
                        // The message is incomplete and there's no more data to process.
                        throw new InvalidDataException("Incomplete message.");
                     }
                     break;
                  }
               }
               catch (InvalidNetworkMessageException ex)
               {
                  throw;
               }
               finally
               {
                  // Since all messages in the buffer are being processed, you can use the
                  // remaining buffer's Start and End position to determine consumed and examined.
                  reader.AdvanceTo(this.consumed, this.examined);
                  //reader.AdvanceTo(buffer.Start, buffer.End);
               }
            }
         }
         finally
         {
            await reader.CompleteAsync().ConfigureAwait(false);
         }
      }


      private bool TryReadNetworkMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> rawMessage)
      {
         // Look for a EOL in the buffer.
         SequencePosition? position = buffer.PositionOf((byte)'\n');

         if (position == null)
         {
            rawMessage = default;
            return false;
         }

         // Skip the line + the \n.
         rawMessage = buffer.Slice(0, position.Value);
         buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
         return true;
      }

      private async Task ProcessMessageAsync(INetworkMessage message, CancellationToken cancellationToken)
      {
         using IDisposable logScope = this.logger.BeginScope("Processing message '{Command}'", message.Command);
         this.logger.LogDebug("Parsing message '{Command}'", message.Command);
         if (message is UnknownMessage)
         {
            this.logger.LogWarning("Deserializer for message '{Command}' not found.", message.Command);
         }
         else
         {
            this.logger.LogDebug(JsonSerializer.Serialize(message, message.GetType(), new JsonSerializerOptions { WriteIndented = true }));
            this.eventBus.Publish(new PeerMessageReceived(this.peerConnection.PeerContext, message, this.networkMessageDecoder.ContextData.GetTotalMessageLength()));

            await this.peerConnection.PeerContext.ProcessMessageAsync(message).ConfigureAwait(false);
         }


         this.stateMachine.Fire(PeerConnectionTrigger.WaitMessage);
      }

      private void Disconnected()
      {
         this.logger.LogDebug("Peer {PeerConnectionId} Disconnected", this.peerConnection.PeerContext);
      }

      private Task DisconnectingAsync(string reason, Exception ex, CancellationToken cancellationToken)
      {
         this.logger.LogDebug(ex, "Disconnecting {PeerConnectionId}: {Reason}", this.peerConnection.PeerContext, reason);
         this.peerConnection.ConnectedClient.Close();
         this.eventBus.Publish(new PeerDisconnected(this.peerConnection.PeerContext, reason, ex));
         this.stateMachine.Fire(PeerConnectionTrigger.PeerDisconnected);
         return Task.CompletedTask;
      }


      /// <summary>
      /// Enables the state transition logs.
      /// </summary>
      /// <remarks>Transition logs are enabled only if the code is compiled with DEBUG symbol</remarks>
      [Conditional("DEBUG")]
      private void EnableStateTransitionLogs()
      {
         this.stateMachine.OnTransitioned(transition =>
         {
            this.logger.LogDebug("From {FromState} to {ToState}", transition.Source, transition.Destination);
         });
      }
   }
}
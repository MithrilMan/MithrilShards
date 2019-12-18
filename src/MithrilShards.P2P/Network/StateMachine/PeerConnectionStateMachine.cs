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
using Stateless;

namespace MithrilShards.P2P.Network.StateMachine {
   public class PeerConnectionStateMachine {
      readonly StateMachine<PeerConnectionState, PeerConnectionTrigger> stateMachine;
      private readonly Guid machineId;
      readonly ILogger logger;
      readonly IEventBus eventBus;
      readonly PeerConnectionDirection peerConnectionDirection;
      readonly TcpClient connectedClient;
      readonly CancellationToken cancellationToken;

      readonly IPEndPoint remoteEndPoint;

      readonly ForgeNetwork network;
      private PipeWriter pipeWriter;
      private PipeReader pipeReader;

      public PeerConnectionState Status { get => this.stateMachine.State; }


      public PeerConnectionStateMachine(ILogger logger, IEventBus eventBus, PeerConnectionDirection peerConnectionDirection, TcpClient connectedClient, CancellationToken cancellationToken) {
         this.logger = logger;
         this.eventBus = eventBus;
         this.peerConnectionDirection = peerConnectionDirection;
         this.connectedClient = connectedClient;
         this.cancellationToken = cancellationToken;
         this.stateMachine = new StateMachine<PeerConnectionState, PeerConnectionTrigger>(PeerConnectionState.Initializing);

         this.machineId = Guid.NewGuid();

         this.remoteEndPoint = this.connectedClient.Client.RemoteEndPoint as IPEndPoint;

         this.ConfigureStateMachine();
      }

      private void ConfigureStateMachine() {

         this.stateMachine.Configure(PeerConnectionState.Initializing)
            .Permit(PeerConnectionTrigger.Connect, PeerConnectionState.Connecting)
            .Permit(PeerConnectionTrigger.AcceptConnection, PeerConnectionState.Connected)
            .Permit(PeerConnectionTrigger.Cancel, PeerConnectionState.Cancelled);

         this.stateMachine.Configure(PeerConnectionState.Connecting)
            .Permit(PeerConnectionTrigger.Connected, PeerConnectionState.Connected)
            .Permit(PeerConnectionTrigger.ConnectionFail, PeerConnectionState.ConnectionFailed)
            .OnEntryAsync(async () => await this.StartReceivingMessages().ConfigureAwait(false));

         this.stateMachine.OnUnhandledTrigger((state, trigger) => {
            this.logger.LogWarning("Unhandled Trigger: '{State}' state, '{trigger}' trigger!", state, trigger);
         });
      }

      public async Task AcceptOutgoingConnection() {
         await this.stateMachine.FireAsync(PeerConnectionTrigger.Connect).ConfigureAwait(false);
      }

      /// <summary>
      /// Reads messages from the connection stream.
      /// </summary>
      private async Task StartReceivingMessages() {
         var pipe = new Pipe();
         this.pipeWriter = pipe.Writer;
         this.pipeReader = pipe.Reader;

         try {
            while (!this.cancellationToken.IsCancellationRequested) {

               // reading data from a pipe instance
               ReadResult result = await this.pipeReader.ReadAsync(this.cancellationToken);
               ReadOnlySequence<byte> buffer = result.Buffer;

               SequencePosition? position = null;

               this.connectedClient.GetStream();

               // We perform calculations with the data obtained.
               await _bytesProcessor.ProcessBytesAsync(buffer, token);


               //(Message message, int rawMessageSize) = await this.ReadAndParseMessageAsync(this.peer.Version).ConfigureAwait(false);

               //this.eventBus.Publish(new PeerMessageReceived(this.remoteEndPoint, message, rawMessageSize));
               //this.logger.LogDebug("Received message: '{Message}'", message);

               //this.peer.Counter.AddRead(rawMessageSize);

               //var incomingMessage = new IncomingMessage() {
               //   Message = message,
               //   Length = rawMessageSize,
               //};

               //this.MessageProducer.PushMessage(incomingMessage);
            }
         }
         catch (Exception ex) when (ex is IOException || ex is OperationCanceledException || ex is ObjectDisposedException) {
            this.logger.LogDebug(ex, "The node stopped receiving messages: {ErrorMessage}", ex.Message);
            await this.stateMachine.FireAsync(PeerConnectionTrigger.Disconnect, "The node stopped receiving messages", ex).ConfigureAwait(false);
            this.peer.Disconnect("The node stopped receiving messages.", ex);
         }
         catch (Exception ex) {
            this.logger.LogDebug("Unexpected failure whilst receiving messages, exception: {0}", ex.ToString());
            this.peer.Disconnect($"Unexpected failure whilst receiving messages.", ex);
         }
      }



      /// <summary>
      /// Reads a raw binary message from the connection stream and formats it to a structured message.
      /// </summary>
      /// <param name="protocolVersion">Version of the protocol that defines the message format.</param>
      /// <returns>
      /// Binary message received from the connected counter-party and the size of the raw message in bytes.
      /// </returns>
      /// <exception cref="OperationCanceledException">Thrown if the operation was canceled or the end of the stream was reached.</exception>
      /// <exception cref="FormatException">Thrown if the incoming message is too big.</exception>
      /// <exception cref="ObjectDisposedException">Thrown if the connection has been disposed.</exception>
      /// <exception cref="IOException">Thrown if the I/O operation has been aborted because of either a thread exit or an application request.</exception>
      /// <remarks>
      /// TODO: Currently we rely on <see cref="Message.ReadNext(System.IO.Stream, Network, ProtocolVersion, CancellationToken, byte[], out PerformanceCounter)" />
      /// for parsing the message from binary data. That method need stream to read from, so to achieve that we create a memory stream from our data,
      /// which is not efficient. This should be improved.
      /// </remarks>
      private async Task<(Message message, int rawMessageSize)> ReadAndParseMessageAsync(ProtocolVersion protocolVersion) {
         Message message = null;

         byte[] rawMessage = await this.ReadMessageAsync(protocolVersion).ConfigureAwait(false);
         using (var memoryStream = new MemoryStream(rawMessage)) {
            //TODO
            //message = Message.ReadNext(memoryStream, this.network, protocolVersion, this.cancellationToken, this.payloadProvider, out PerformanceCounter counter);
         }

         return (message, rawMessage.Length);
      }

      /// <summary>
      /// Reads raw message in binary form from the connection stream.
      /// </summary>
      /// <param name="protocolVersion">Version of the protocol that defines the message format.</param>
      /// <returns>
      /// Binary message received from the connected counter-party.
      /// </returns>
      /// <exception cref="ProtocolViolationException">Thrown if the incoming message is too big.</exception>
      /// <exception cref="OperationCanceledException">Thrown if the operation was canceled or the end of the stream was reached.</exception>
      private async Task<byte[]> ReadMessageAsync(ProtocolVersion protocolVersion) {
         // First find and read the magic.
         await this.ReadMagicAsync(this.network.MagicBytes).ConfigureAwait(false);

         // Then read the header, which is formed of command, length, and possibly also a checksum.
         int checksumSize = protocolVersion >= ProtocolVersion.MEMPOOL_GD_VERSION ? Message.ChecksumSize : 0;
         int headerSize = Message.CommandSize + Message.LengthSize + checksumSize;

         byte[] messageHeader = new byte[headerSize];
         await this.ReadBytesAsync(messageHeader, 0, headerSize).ConfigureAwait(false);

         // Then extract the length, which is the message payload size.
         int lengthOffset = Message.CommandSize;
         uint length = BitConverter.ToUInt32(messageHeader, lengthOffset);

         // 4 MB limit on message size.
         // Limit is based on the largest valid object that we can receive which is the block.
         // Max size of a block on segwit-enabled network is 4mb.
         if (length > 0x00400000) {
            throw new ProtocolViolationException("Message payload too big (over 0x00400000 bytes)");
         }

         // Read the payload.
         int magicLength = this.network.MagicBytes.Length;
         byte[] message = new byte[magicLength + headerSize + length];

         await this.ReadBytesAsync(message, magicLength + headerSize, (int)length).ConfigureAwait(false);

         // And copy the magic and the header to form a complete message.
         Array.Copy(this.network.MagicBytes, 0, message, 0, this.network.MagicBytes.Length);
         Array.Copy(messageHeader, 0, message, this.network.MagicBytes.Length, headerSize);

         return message;
      }

      /// <summary>
      /// Seeks and reads the magic value from the connection stream.
      /// </summary>
      /// <param name="magic">Magic value that starts the message.</param>
      /// <exception cref="OperationCanceledException">Thrown if the operation was canceled or the end of the stream was reached.</exception>
      /// <remarks>
      /// Each network message starts with the magic value. If the connection stream is in unknown state,
      /// the next bytes to read might not be the magic. Therefore we read from the stream until we find the magic value.
      /// </remarks>
      private async Task ReadMagicAsync(byte[] magic) {
         byte[] bytes = new byte[1];
         for (int i = 0; i < magic.Length; i++) {
            byte expectedByte = magic[i];

            await this.ReadBytesAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

            byte receivedByte = bytes[0];
            if (expectedByte != receivedByte) {
               // If we did not receive the next byte we expected
               // we either received the first byte of the magic value
               // or not. If yes, we set index to 0 here, which is then
               // incremented in for loop to 1 and we thus continue
               // with the second byte. Otherwise, we set index to -1
               // here, which means that after the loop incrementation,
               // we will start from first byte of magic.
               i = receivedByte == magic[0] ? 0 : -1;
            }
         }
      }

      /// <summary>
      /// Reads a specific number of bytes from the connection stream into a buffer.
      /// </summary>
      /// <param name="buffer">Buffer to read incoming data to.</param>
      /// <param name="offset">Position in the buffer where to write the data.</param>
      /// <param name="bytesToRead">Number of bytes to read.</param>
      /// <exception cref="OperationCanceledException">Thrown if the operation was canceled or the end of the stream was reached.</exception>
      private async Task ReadBytesAsync(byte[] buffer, int offset, int bytesToRead) {
         NetworkStream innerStream = this.stream;

         if (innerStream == null) {
            this.logger.LogDebug("Connection has been terminated.");
            this.logger.LogTrace("(-)[NO_STREAM]");
            throw new OperationCanceledException();
         }

         while (bytesToRead > 0) {
            int chunkSize = await innerStream.ReadAsync(buffer, offset, bytesToRead, this.cancellationToken).ConfigureAwait(false);
            if (chunkSize == 0) {
               this.logger.LogTrace("(-)[STREAM_END]");
               throw new OperationCanceledException();
            }

            offset += chunkSize;
            bytesToRead -= chunkSize;
         }
      }
   }

   internal class ForgeNetwork {
      public byte[] MagicBytes { get; internal set; }
   }
}

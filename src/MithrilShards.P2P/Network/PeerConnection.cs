using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.P2P.Network.StateMachine;

namespace MithrilShards.P2P.Network {

   public class PeerConnection {
      /// <summary>Instance logger.</summary>
      private readonly ILogger logger;

      /// <summary>Provider of time functions.</summary>
      private readonly IDateTimeProvider dateTimeProvider;
      readonly TcpClient connectedClient;
      private readonly PeerConnectionDirection peerConnectionDirection;
      readonly CancellationToken cancellationToken;
      readonly PeerConnectionStateMachine connectionStateMachine;
      private PeerConnectionState State;

      public TimeSpan? TimeOffset { get; private set; }

      public PeerDisconnectionReason DisconnectReason { get; private set; }

      public PeerConnection(ILogger<PeerConnection> logger,
                            IDateTimeProvider dateTimeProvider,
                            TcpClient connectedClient,
                            PeerConnectionDirection peerConnectionDirection,
                            CancellationToken cancellationToken) {
         this.logger = logger;
         this.dateTimeProvider = dateTimeProvider;
         this.connectedClient = connectedClient;
         this.peerConnectionDirection = peerConnectionDirection;
         this.cancellationToken = cancellationToken;

         this.connectionStateMachine = new PeerConnectionStateMachine(logger, peerConnectionDirection, connectedClient, cancellationToken);
      }

      /// <inheritdoc/>
      public async Task ConnectAsync(CancellationToken cancellation = default(CancellationToken)) {
         try {
            this.connectionStateMachine.AcceptOutgoingConnection();
         }
         catch (OperationCanceledException) {
            this.logger.LogDebug("Connection to '{0}' cancelled.", this.peerEndPoint);

            await this.SetStateAsync(NetworkPeerState.Offline).ConfigureAwait(false);

            this.logger.LogTrace("(-)[CANCELLED]");
            throw;
         }
         catch (Exception ex) {
            this.logger.LogDebug("Exception occurred while connecting to peer '{0}': {1}", this.peerEndPoint, ex is SocketException ? ex.Message : ex.ToString());

            this.DisconnectReason = new NetworkPeerDisconnectReason() {
               Reason = "Unexpected exception while connecting to socket",
               Exception = ex
            };

            await this.SetStateAsync(NetworkPeerState.Failed).ConfigureAwait(false);

            this.logger.LogTrace("(-)[EXCEPTION]");
            throw;
         }
      }

      /// <summary>
      /// Calls event handlers when the network state of the peer is changed.
      /// </summary>
      /// <param name="previous">Previous network state of the peer.</param>
      private async Task OnStateChangedAsync(NetworkPeerState previous) {
         bool insideCallback = this.onDisconnectedAsyncContext.Value == null;
         if (!insideCallback)
            this.onDisconnectedAsyncContext.Value = new DisconnectedExecutionAsyncContext();

         try {
            await this.StateChanged.ExecuteCallbacksAsync(this, previous).ConfigureAwait(false);
         }
         catch (Exception e) {
            this.logger.LogError("Exception occurred while calling state changed callbacks: {0}", e.ToString());
            throw;
         }
         finally {
            if (!insideCallback) {
               if (this.onDisconnectedAsyncContext.Value.DisconnectCallbackRequested)
                  this.onDisconnected(this);

               this.onDisconnectedAsyncContext.Value = null;
            }
         }
      }

      /// <summary>
      /// Processes an incoming message from the peer and calls subscribed event handlers.
      /// </summary>
      /// <param name="message">Message received from the peer.</param>
      /// <param name="cancellation">Cancellation token to abort message processing.</param>
      private async Task ProcessMessageAsync(IncomingMessage message, CancellationToken cancellation) {
         try {
            switch (message.Message.Payload) {
               case VersionPayload versionPayload:
                  await this.ProcessVersionMessageAsync(versionPayload, cancellation).ConfigureAwait(false);
                  break;

               case HaveWitnessPayload unused:
                  this.SupportedTransactionOptions |= TransactionOptions.Witness;
                  break;
            }
         }
         catch {
            this.logger.LogDebug("Exception occurred while processing a message from the peer. Connection has been closed and message won't be processed further.");
            this.logger.LogTrace("(-)[EXCEPTION_PROCESSING]");
            return;
         }

         try {
            this.onDisconnectedAsyncContext.Value = new DisconnectedExecutionAsyncContext();

            await this.MessageReceived.ExecuteCallbacksAsync(this, message).ConfigureAwait(false);
         }
         catch (Exception e) {
            this.logger.LogCritical("Exception occurred while calling message received callbacks: {0}", e.ToString());
            this.logger.LogTrace("(-)[EXCEPTION_CALLBACKS]");
            throw;
         }
         finally {
            if (this.onDisconnectedAsyncContext.Value.DisconnectCallbackRequested)
               this.onDisconnected(this);

            this.onDisconnectedAsyncContext.Value = null;
         }
      }

      /// <summary>
      /// Processes a "version" message received from a peer.
      /// </summary>
      /// <param name="version">Version message received from a peer.</param>
      /// <param name="cancellation">Cancellation token to abort message processing.</param>
      private async Task ProcessVersionMessageAsync(VersionPayload version, CancellationToken cancellation) {
         this.logger.LogDebug("Peer's state is {0}.", this.State);

         switch (this.State) {
            case NetworkPeerState.Connected:
               if (this.Inbound)
                  await this.ProcessInitialVersionPayloadAsync(version, cancellation).ConfigureAwait(false);

               break;

            case NetworkPeerState.HandShaked:
               if (this.Version >= ProtocolVersion.REJECT_VERSION) {
                  var rejectPayload = new RejectPayload() {
                     Code = RejectCode.DUPLICATE
                  };

                  await this.SendMessageAsync(rejectPayload, cancellation).ConfigureAwait(false);
               }

               break;
         }

         this.TimeOffset = this.dateTimeProvider.GetTimeOffset() - version.Timestamp;
         if ((version.Services & NetworkPeerServices.NODE_WITNESS) != 0)
            this.SupportedTransactionOptions |= TransactionOptions.Witness;
      }

      /// <summary>
      /// Processes an initial "version" message received from a peer.
      /// </summary>
      /// <param name="version">Version message received from a peer.</param>
      /// <param name="cancellation">Cancellation token to abort message processing.</param>
      /// <exception cref="OperationCanceledException">Thrown if the response to our "version" message is not received on time.</exception>
      private async Task ProcessInitialVersionPayloadAsync(VersionPayload version, CancellationToken cancellation) {
         this.PeerVersion = version;
         bool connectedToSelf = version.Nonce == this.ConnectionParameters.Nonce;

         this.logger.LogDebug("First message received from peer '{0}'.", version.AddressFrom);

         if (connectedToSelf) {
            this.logger.LogDebug("Connection to self detected, disconnecting.");

            this.Disconnect("Connected to self");
            this.selfEndpointTracker.Add(version.AddressReceiver);

            this.logger.LogTrace("(-)[CONNECTED_TO_SELF]");
            throw new OperationCanceledException();
         }

         using (CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.Connection.CancellationSource.Token, cancellation)) {
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(10.0));
            try {
               await this.RespondToHandShakeAsync(cancellationSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) {
               if (ex.CancellationToken == cancellationSource.Token) {
                  this.logger.LogDebug("Remote peer hasn't responded within 10 seconds of the handshake completion, dropping connection.");
                  this.Disconnect("Handshake timeout");
               }
               else {
                  this.logger.LogDebug("Handshake problem, dropping connection. Problem: '{0}'.", ex.Message);
                  this.Disconnect($"Handshake problem, reason: '{ex.Message}'.");
               }

               this.logger.LogTrace("(-)[HANDSHAKE_TIMEDOUT]");
               throw;
            }
            catch (Exception ex) {
               this.logger.LogDebug("Exception occurred: {0}", ex.ToString());

               this.Disconnect("Handshake exception", ex);

               this.logger.LogTrace("(-)[HANDSHAKE_EXCEPTION]");
               throw;
            }
         }
      }

      /// <summary>
      /// Initializes behaviors from the default template.
      /// </summary>
      /// <param name="parameters">Various settings and requirements related to how the connections with peers are going to be established, including the default behaviors template.</param>
      private void InitDefaultBehaviors(NetworkPeerConnectionParameters parameters) {
         this.advertize = parameters.Advertize;
         this.preferredTransactionOptions = parameters.PreferredTransactionOptions;

         foreach (INetworkPeerBehavior behavior in parameters.TemplateBehaviors) {
            this.Behaviors.Add(behavior.Clone());
         }

         if ((this.State == NetworkPeerState.Connected) || (this.State == NetworkPeerState.HandShaked)) {
            foreach (INetworkPeerBehavior behavior in this.Behaviors) {
               behavior.Attach(this);
            }
         }
      }

      /// <inheritdoc/>
      public void SendMessage(Payload payload) {
         Guard.NotNull(payload, nameof(payload));

         if (!this.IsConnected) {
            this.logger.LogTrace("(-)[NOT_CONNECTED]");
            throw new OperationCanceledException("The peer has been disconnected");
         }

         this.asyncPayloadsQueue.Enqueue(payload);
      }

      /// <summary>
      /// This is used by the <see cref="asyncPayloadsQueue"/> to send payloads messages to peers under a separate thread.
      /// If a message is sent inside the state change even and the send fails this could cause a deadlock,
      /// to avoid that if there is any danger of a deadlock it better to use the SendMessage method and go via the queue.
      /// </summary>
      private async Task SendMessageHandledAsync(Payload payload, CancellationToken cancellation = default(CancellationToken)) {
         try {
            await this.SendMessageAsync(payload, cancellation);
         }
         catch (OperationCanceledException) {
            this.logger.LogDebug("Connection to '{0}' cancelled.", this.peerEndPoint);
         }
         catch (Exception ex) {
            this.logger.LogError("Exception occurred while connecting to peer '{0}': {1}", this.peerEndPoint, ex is SocketException ? ex.Message : ex.ToString());
            throw;
         }
      }

      /// <inheritdoc/>
      public async Task SendMessageAsync(Payload payload, CancellationToken cancellation = default(CancellationToken)) {
         Guard.NotNull(payload, nameof(payload));

         if (!this.IsConnected) {
            this.logger.LogTrace("(-)[NOT_CONNECTED]");
            throw new OperationCanceledException("The peer has been disconnected");
         }

         this.onSendingMessage?.Invoke(this.RemoteSocketEndpoint, payload);

         await this.Connection.SendAsync(payload, cancellation).ConfigureAwait(false);
      }

      /// <inheritdoc/>
      public async Task VersionHandshakeAsync(CancellationToken cancellationToken = default(CancellationToken)) {
         await this.VersionHandshakeAsync(null, cancellationToken).ConfigureAwait(false);
      }

      /// <inheritdoc/>
      public async Task VersionHandshakeAsync(NetworkPeerRequirement requirements, CancellationToken cancellationToken) {
         // In stratisX, the equivalent functionality is contained in main.cpp, method ProcessMessage()

         requirements = requirements ?? new NetworkPeerRequirement();
         using (var listener = new NetworkPeerListener(this, this.asyncProvider)) {
            this.logger.LogDebug("Sending my version.");
            await this.SendMessageAsync(this.MyVersion, cancellationToken).ConfigureAwait(false);

            this.logger.LogDebug("Waiting for version or rejection message.");
            bool versionReceived = false;
            bool verAckReceived = false;
            while (!versionReceived || !verAckReceived) {
               Payload payload = await listener.ReceivePayloadAsync<Payload>(cancellationToken).ConfigureAwait(false);
               switch (payload) {
                  case RejectPayload rejectPayload:
                     this.logger.LogTrace("(-)[HANDSHAKE_REJECTED]");
                     throw new ProtocolException("Handshake rejected: " + rejectPayload.Reason);

                  case VersionPayload versionPayload:
                     versionReceived = true;

                     this.PeerVersion = versionPayload;
                     if (!versionPayload.AddressReceiver.Address.Equals(this.MyVersion.AddressFrom.Address)) {
                        this.logger.LogDebug("Different external address detected by the node '{0}' instead of '{1}'.", versionPayload.AddressReceiver.Address, this.MyVersion.AddressFrom.Address);
                     }

                     if (versionPayload.Version < ProtocolVersion.MIN_PEER_PROTO_VERSION) {
                        this.logger.LogDebug("Outdated version {0} received, disconnecting peer.", versionPayload.Version);

                        this.Disconnect("Outdated version");
                        this.logger.LogTrace("(-)[OUTDATED]");
                        return;
                     }

                     if (!requirements.Check(versionPayload, this.Inbound, out string reason)) {
                        this.logger.LogTrace("(-)[UNSUPPORTED_REQUIREMENTS]");
                        this.Disconnect("The peer does not support the required services requirement, reason: " + reason);
                        return;
                     }

                     this.logger.LogDebug("Sending version acknowledgement.");
                     await this.SendMessageAsync(new VerAckPayload(), cancellationToken).ConfigureAwait(false);
                     this.selfEndpointTracker.UpdateAndAssignMyExternalAddress(versionPayload.AddressFrom, false);
                     break;

                  case VerAckPayload verAckPayload:
                     verAckReceived = true;
                     break;
               }
            }

            await this.SetStateAsync(NetworkPeerState.HandShaked).ConfigureAwait(false);

            if (this.advertize && this.MyVersion.AddressFrom.Address.IsRoutable(true)) {
               var addrPayload = new AddrPayload
               (
                   new NetworkAddress(this.MyVersion.AddressFrom) {
                      Time = this.dateTimeProvider.GetTimeOffset()
                   }
               );

               await this.SendMessageAsync(addrPayload, cancellationToken).ConfigureAwait(false);
            }

            // Ask the just-handshaked peer for the peers they know about to aid in our own peer discovery.
            await this.SendMessageAsync(new GetAddrPayload(), cancellationToken).ConfigureAwait(false);
         }
      }

      /// <inheritdoc/>
      public async Task RespondToHandShakeAsync(CancellationToken cancellationToken = default(CancellationToken)) {
         using (var listener = new NetworkPeerListener(this, this.asyncProvider)) {
            this.logger.LogDebug("Responding to handshake with my version.");
            await this.SendMessageAsync(this.MyVersion, cancellationToken).ConfigureAwait(false);

            this.logger.LogDebug("Waiting for version acknowledgement or rejection message.");

            while (this.State != NetworkPeerState.HandShaked) {
               Payload payload = await listener.ReceivePayloadAsync<Payload>(cancellationToken).ConfigureAwait(false);
               switch (payload) {
                  case RejectPayload rejectPayload:
                     this.logger.LogDebug("Version rejected: code {0}, reason '{1}'.", rejectPayload.Code, rejectPayload.Reason);
                     this.logger.LogTrace("(-)[VERSION_REJECTED]");
                     throw new ProtocolException("Version rejected " + rejectPayload.Code + ": " + rejectPayload.Reason);

                  case VerAckPayload verAckPayload:
                     this.logger.LogDebug("Sending version acknowledgement.");
                     await this.SendMessageAsync(new VerAckPayload(), cancellationToken).ConfigureAwait(false);
                     await this.SetStateAsync(NetworkPeerState.HandShaked).ConfigureAwait(false);
                     break;
               }
            }
         }
      }

      /// <inheritdoc/>
      public void Disconnect(string reason, Exception exception = null) {
         if (Interlocked.CompareExchange(ref this.disconnected, 1, 0) == 1) {
            this.logger.LogTrace("(-)[DISCONNECTED]");
            return;
         }

         if (this.IsConnected) this.SetStateAsync(NetworkPeerState.Disconnecting).GetAwaiter().GetResult();

         this.Connection.Disconnect();

         if (this.DisconnectReason == null) {
            this.DisconnectReason = new NetworkPeerDisconnectReason() {
               Reason = reason,
               Exception = exception
            };
         }

         NetworkPeerState newState = exception == null ? NetworkPeerState.Offline : NetworkPeerState.Failed;
         this.SetStateAsync(newState).GetAwaiter().GetResult();
      }

      /// <summary>
      /// Executes <see cref="onDisconnected"/> callback if no callbacks are currently executing in the same async context,
      /// schedules <see cref="onDisconnected"/> execution after the callback otherwise.
      /// </summary>
      private void ExecuteDisconnectedCallbackWhenSafe() {
         if (this.onDisconnected != null) {
            // Value wasn't set in this async context, which means that we are outside of the callbacks execution and it is allowed to call `onDisconnected`.
            if (this.onDisconnectedAsyncContext.Value == null) {
               this.logger.LogDebug("Disconnection callback is being executed.");
               this.onDisconnected(this);
            }
            else {
               this.logger.LogDebug("Disconnection callback is scheduled for execution when other callbacks are finished.");
               this.onDisconnectedAsyncContext.Value.DisconnectCallbackRequested = true;
            }
         }
         else
            this.logger.LogDebug("Disconnection callback is not specified.");
      }

      /// <inheritdoc />
      public void Dispose() {
         if (Interlocked.CompareExchange(ref this.disposed, 1, 0) == 1) {
            this.logger.LogTrace("(-)[DISPOSED]");
            return;
         }

         this.Disconnect("Peer disposed");

         this.logger.LogDebug("Behaviors detachment started.");

         foreach (INetworkPeerBehavior behavior in this.Behaviors) {
            try {
               behavior.Detach();
               behavior.Dispose();
            }
            catch (Exception ex) {
               this.logger.LogError("Error while detaching behavior '{0}': {1}", behavior.GetType().FullName, ex.ToString());
            }
         }

         this.asyncPayloadsQueue.Dispose();
         this.Connection.Dispose();

         this.MessageReceived.Dispose();
         this.StateChanged.Dispose();
      }

      /// <inheritdoc />
      public InventoryType AddSupportedOptions(InventoryType inventoryType) {
         // Transaction options we prefer and which are also supported by peer.
         TransactionOptions actualTransactionOptions = this.preferredTransactionOptions & this.SupportedTransactionOptions;

         if ((actualTransactionOptions & TransactionOptions.Witness) != 0)
            inventoryType |= InventoryType.MSG_WITNESS_FLAG;

         return inventoryType;
      }

      /// <inheritdoc />
      [NoTrace]
      public T Behavior<T>() where T : INetworkPeerBehavior {
         return this.Behaviors.OfType<T>().FirstOrDefault();
      }
   }
}

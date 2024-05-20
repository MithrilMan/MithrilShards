using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors;

/// <summary>Initializes a new instance of the <see cref="BaseProcessor"/> class.</summary>
/// <param name="logger">The logger.</param>
/// <param name="eventBus">The event bus.</param>
/// <param name="peerBehaviorManager">The peer behavior manager.</param>
/// <param name="isHandshakeAware">If set to <c>true</c> register the instance to be handshake aware: when the peer is handshaked, OnPeerHandshaked method will be invoked.</param>
/// <param name="receiveMessagesOnlyIfHandshaked">if set to <c>true</c> receives messages only if handshaked.</param>
public abstract class BaseProcessor(
   ILogger<BaseProcessor> logger,
   IEventBus eventBus,
   IPeerBehaviorManager peerBehaviorManager,
   bool isHandshakeAware,
   bool receiveMessagesOnlyIfHandshaked
   ) : INetworkMessageProcessor
{
   protected readonly ILogger<BaseProcessor> logger = logger;
   protected readonly IEventBus eventBus = eventBus;
   private bool _isHandshaked = false;
   private INetworkMessageWriter _messageWriter = null!; //hack to not rising null warnings, these are initialized when calling AttachAsync

   /// <summary>
   /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
   /// </summary>
   private readonly EventSubscriptionManager _eventSubscriptionManager = new();

   public BitcoinPeerContext PeerContext { get; private set; } = null!; //hack to not rising null warnings, these are initialized when calling AttachAsync

   public virtual bool Enabled { get; private set; } = true;

   /// <inheritdoc/>
   public virtual bool CanReceiveMessages => _isHandshaked || receiveMessagesOnlyIfHandshaked == false;

   public async ValueTask AttachAsync(IPeerContext peerContext)
   {
      PeerContext = peerContext as BitcoinPeerContext ?? throw new ArgumentException("Expected BitcoinPeerContext", nameof(peerContext));
      _messageWriter = PeerContext.GetMessageWriter();

      await OnPeerAttachedAsync().ConfigureAwait(false);
   }

   /// <summary>
   /// Called when a peer has been attached and <see cref="PeerContext"/> assigned.
   /// </summary>
   /// <returns></returns>
   protected virtual ValueTask OnPeerAttachedAsync()
   {
      RegisterLifeTimeEventHandler<PeerHandshaked>(async (receivedEvent) =>
      {
         _isHandshaked = true;

         if (isHandshakeAware)
         {
            await OnPeerHandshakedAsync().ConfigureAwait(false);
         }
      }, IsCurrentPeer);


      return default;
   }

   /// <summary>
   /// Method invoked when the peer handshakes and isHandshakeAware is set to <see langword="true" />.
   /// </summary>
   /// <returns></returns>
   protected virtual ValueTask OnPeerHandshakedAsync()
   {
      return default;
   }

   /// <summary>
   /// Registers a <see cref="IEventBus" /> message <paramref name="handler" /> that will be automatically
   /// unregistered once the component gets disposed.
   /// </summary>
   /// <typeparam name="TEventBase">The type of the event base.</typeparam>
   /// <param name="handler">The handler.</param>
   /// <param name="clause">The clause.</param>
   protected void RegisterLifeTimeEventHandler<TEventBase>(Func<TEventBase, ValueTask> handler, Func<TEventBase, bool>? clause = null) where TEventBase : EventBase
   {
      _eventSubscriptionManager.RegisterSubscriptions(eventBus.Subscribe<TEventBase>(async (message, cancellationToken) =>
      {
         // ensure we listen only to events we are interested into
         if (clause != null && !clause(message)) return;

         await handler(message).ConfigureAwait(false);
      }));
   }

   /// <summary>
   /// Sends the message asynchronously to the other peer.
   /// </summary>
   /// <param name="message">The message to send.</param>
   /// <param name="cancellationToken">The cancellation token.</param>
   /// <returns></returns>
   protected async ValueTask SendMessageAsync(INetworkMessage message, CancellationToken cancellationToken = default)
   {
      await SendMessageAsync(PeerContext.NegotiatedProtocolVersion.Version, message, cancellationToken).ConfigureAwait(false);
   }

   /// <summary>
   /// Sends the message asynchronously to the other peer.
   /// </summary>
   /// <param name="message">The message to send.</param>
   /// <param name="minVersion">
   /// The minimum, inclusive, negotiated version required to send this message.
   /// Passing 0 means the message will be sent without version check.
   /// </param>
   /// <param name="cancellationToken">The cancellation token.</param>
   /// <returns></returns>
   protected async ValueTask<bool> SendMessageAsync(int minVersion, INetworkMessage message, CancellationToken cancellationToken = default)
   {
      if (PeerContext.NegotiatedProtocolVersion.Version < minVersion)
      {
         logger.LogDebug("Can't send message, negotiated protocol version is below required protocol.");
         return false;
      }

      //TODO: handshake shouldn't be involved in this check
      //if (!this.PeerContext.IsConnected)
      //{
      //   this.logger.LogDebug("Can't send message, the peer is not in a connected state.");
      //   return false;
      //}

      await _messageWriter.WriteAsync(message, cancellationToken).ConfigureAwait(false);
      return true;
   }

   /// <summary>
   /// Disconnects if, after timeout expires, the condition is evaluated to true.
   /// </summary>
   /// <param name="condition">The condition that, when evaluated to true, causes the peer to be disconnected.</param>
   /// <param name="timeout">The timeout that will trigger the condition evaluation.</param>
   /// <param name="reason">The disconnection reason to set in case of disconnection.</param>
   /// <param name="cancellation">The cancellation that may interrupt the <paramref name="condition" /> evaluation.</param>
   /// <returns></returns>
   protected Task DisconnectIfAsync(Func<ValueTask<bool>> condition, TimeSpan timeout, string reason, CancellationToken cancellation = default)
   {
      if (cancellation == default)
      {
         cancellation = PeerContext.ConnectionCancellationTokenSource.Token;
      }

      return Task.Run(async () =>
      {
         try
         {
            await Task.Delay(timeout, cancellation).ConfigureAwait(false);
         }
         catch (OperationCanceledException)
         {
            // Task canceled, legit, ignoring exception.
         }

         // if cancellation was requested, return without doing anything
         if (!cancellation.IsCancellationRequested && !PeerContext.ConnectionCancellationTokenSource.Token.IsCancellationRequested && await condition().ConfigureAwait(false))
         {
            PeerContext.Disconnect(reason);
         }
      }, cancellation);
   }

   /// <summary>
   /// Execute an action in case the condition evaluates to true after <paramref name="timeout" /> expires.
   /// </summary>
   /// <param name="condition">The condition that, when evaluated to true, causes the peer to be disconnected.</param>
   /// <param name="timeout">The timeout that will trigger the condition evaluation.</param>
   /// <param name="action">The condition that, when evaluated to true, causes the peer to be disconnected.</param>
   /// <param name="cancellation">The cancellation.</param>
   /// <returns></returns>
   protected Task ExecuteIfAsync(Func<ValueTask<bool>> condition, TimeSpan timeout, Func<ValueTask> action, CancellationToken cancellation = default)
   {
      if (cancellation == default)
      {
         cancellation = PeerContext.ConnectionCancellationTokenSource.Token;
      }

      return Task.Run(async () =>
      {
         try
         {
            await Task.Delay(timeout, cancellation).ConfigureAwait(false);
         }
         catch (OperationCanceledException)
         {
            // Task canceled, legit, ignoring exception.
         }

         // if cancellation was requested, return without doing anything
         if (!cancellation.IsCancellationRequested && !PeerContext.ConnectionCancellationTokenSource.Token.IsCancellationRequested && await condition().ConfigureAwait(false))
         {
            logger.LogDebug("Condition met, trigger action.");
            await action().ConfigureAwait(false);
         }
      }, cancellation);
   }

   /// <summary>
   /// Punish the peer because of his misbehaves.
   /// </summary>
   /// <param name="penalty">The penalty.</param>
   /// <param name="reason">The reason.</param>
   /// <param name="disconnect">if set to <c>true</c> [disconnect].</param>
   protected void Misbehave(uint penalty, string reason, bool disconnect = false)
   {
      peerBehaviorManager.Misbehave(PeerContext, penalty, reason);
      if (disconnect)
      {
         PeerContext.Disconnect(reason);
      }
   }

   /// <summary>
   /// Returns <see langword="true"/> if the negotiated version supports the specified <paramref name="minVersion"/>.
   /// Returns <see langword="false"/> otherwise.
   /// </summary>
   /// <param name="minVersion">The minimum version that has to be supported in order to return true.</param>
   /// <remarks>
   /// This is useful when there is logic that has to be executed only if a specified version of the negotiated protocol
   /// is supported and would require multiple call of <see cref="SendMessageAsync(int, INetworkMessage, CancellationToken)"/> with the minVersion overload.
   /// </remarks>
   /// <returns></returns>
   protected bool IsSupported(int minVersion)
   {
      return PeerContext.NegotiatedProtocolVersion.Version >= minVersion;
   }

   /// <summary>
   /// Helper methods that returns if a specified received peer events (an event that inherit from <see cref="PeerEventBase"/>) belongs to current peer.
   /// Used usually as the clause for <see cref="RegisterLifeTimeEventHandler"/> to catch only peer events relative to current peer.
   /// </summary>
   /// <param name="theEvent">The event.</param>
   protected bool IsCurrentPeer(PeerEventBase theEvent)
   {
      return theEvent.PeerContext == PeerContext;
   }

   public virtual void Dispose()
   {
      _eventSubscriptionManager.Dispose();
   }
}

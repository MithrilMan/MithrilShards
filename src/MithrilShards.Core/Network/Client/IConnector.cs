using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Core.Network.Client
{
   /// <summary>
   /// Defines the methods required to implement a logic for connecting to external peers
   /// </summary>
   public interface IConnector
   {
      /// <summary>
      /// Sets the connection manager that will be used by the connector.
      /// </summary>
      /// <param name="connectionManager">The connection manager.</param>
      void SetConnectionManager(IConnectionManager connectionManager);

      /// <summary>
      /// Tries to connect to a peer.
      /// </summary>
      /// <returns></returns>
      Task StartConnectionLoopAsync(CancellationToken cancellation);

      /// <summary>
      /// Compute the delay to apply between next connection attempt.
      /// </summary>
      /// <param name="hint">The hint.</param>
      /// <remarks>Override this method to have a custom logic for delayed connection attempts.</remarks>
      /// <returns></returns>
      TimeSpan ComputeDelayAdjustment();
   }
}

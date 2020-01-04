using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;

namespace MithrilShards.Chain.Bitcoin.Protocol
{
   public class SelfConnectionTracker
   {
      readonly ILogger<SelfConnectionTracker> logger;
      readonly IRandomNumberGenerator randomNumberGenerator;
      private readonly List<ulong> localNonces = new List<ulong>();

      public SelfConnectionTracker(ILogger<SelfConnectionTracker> logger, IRandomNumberGenerator randomNumberGenerator)
      {
         this.logger = logger;
         this.randomNumberGenerator = randomNumberGenerator;
      }


      /// <summary>
      /// Determines whether current passed nonce is one of our local nonces (self connection detection).
      /// </summary>
      /// <param name="nonce">The nonce.</param>
      /// <returns>
      ///   <c>true</c> if <paramref name="nonce"/> is one of current local nonces; otherwise, <c>false</c>.
      /// </returns>
      public bool IsSelfConnection(ulong nonce)
      {
         return this.localNonces.Contains(nonce);
      }

      /// <summary>
      /// Generates a new local nonce and add it to the list of known local nonces.
      /// </summary>
      public void AddLocalNonce()
      {
         /// thread safety concerns: currently the process of allocating new nonces is not thread safe but server are generated synchronously,
         /// this mean that we shouldn't have thread problems on this.

         ulong localNonce = this.randomNumberGenerator.GetUint64();
         this.logger.LogDebug("Generated new local nonce {LocalNonce}", localNonce);
         this.localNonces.Add(localNonce);
      }
   }
}

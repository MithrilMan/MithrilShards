using System;

namespace MithrilShards.Core.Network;

/// <summary>
/// Define methods needed by an UserAgentBuilder implementation.
/// UserAgent is meant to be a way to identify the Forge version and shard it uses, where applicable.
/// </summary>
public interface IUserAgentBuilder
{

   /// <summary>
   /// Gets the user agent.
   /// </summary>
   /// <returns></returns>
   /// <exception cref="NotImplementedException"></exception>
   string GetUserAgent();
}

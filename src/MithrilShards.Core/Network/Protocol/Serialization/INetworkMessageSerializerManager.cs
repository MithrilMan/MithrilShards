using System.Collections.Generic;

namespace MithrilShards.Core.Network.Protocol.Serialization {
   public interface INetworkMessageSerializerManager {
      Dictionary<string, INetworkMessageSerializer> Serializers { get; }
   }
}
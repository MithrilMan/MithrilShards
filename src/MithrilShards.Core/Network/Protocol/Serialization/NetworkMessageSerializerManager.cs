using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MithrilShards.Core.Network.Protocol.Serialization {
   public class NetworkMessageSerializerManager : INetworkMessageSerializerManager {
      readonly ILogger<NetworkMessageSerializerManager> logger;
      readonly IEnumerable<INetworkMessageSerializer> messageSerializers;
      public Dictionary<string, INetworkMessageSerializer> Serializers { get; private set; }

      public NetworkMessageSerializerManager(ILogger<NetworkMessageSerializerManager> logger, IEnumerable<INetworkMessageSerializer> messageSerializers) {
         this.logger = logger;
         this.messageSerializers = messageSerializers;

         this.InitializeMessageSerializers();
      }


      private void InitializeMessageSerializers() {
         this.Serializers = (
            from serializer in this.messageSerializers
            let managedMessageType = serializer.GetMessageType()
            let networkMessageAttribute = managedMessageType.GetCustomAttribute<NetworkMessageAttribute>()
            where networkMessageAttribute != null
            select new { Command = networkMessageAttribute.Command, Serializer = serializer }
         ).ToDictionary(reg => reg.Command, reg => reg.Serializer);


         this.logger.LogInformation(
                  "Using {NetworkMessageSerializersCount} message network serializers: {NetworkMessageSerializers}.",
                  this.Serializers.Count,
                  this.Serializers.Keys.ToArray()
                  );
      }
   }
}

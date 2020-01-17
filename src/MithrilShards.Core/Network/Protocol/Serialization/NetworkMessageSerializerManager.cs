using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   public class NetworkMessageSerializerManager : INetworkMessageSerializerManager
   {
      readonly ILogger<NetworkMessageSerializerManager> logger;
      readonly IEnumerable<INetworkMessageSerializer> messageSerializers;
      private Dictionary<string, INetworkMessageSerializer> serializers;

      public NetworkMessageSerializerManager(ILogger<NetworkMessageSerializerManager> logger, IEnumerable<INetworkMessageSerializer> messageSerializers)
      {
         this.logger = logger;
         this.messageSerializers = messageSerializers;
         this.serializers = null!; //will be serialized in InitializeMessageSerializers

         this.InitializeMessageSerializers();
      }


      private void InitializeMessageSerializers()
      {
         this.serializers = (
            from serializer in this.messageSerializers
            let managedMessageType = serializer.GetMessageType()
            let networkMessageAttribute = managedMessageType.GetCustomAttribute<NetworkMessageAttribute>()
            where networkMessageAttribute != null
            select new { Command = networkMessageAttribute.Command, Serializer = serializer }
         ).ToDictionary(reg => reg.Command, reg => reg.Serializer);


         this.logger.LogInformation(
                  "Using {NetworkMessageSerializersCount} message network serializers: {NetworkMessageSerializers}.",
                  this.serializers.Count,
                  this.serializers.Keys.ToArray()
                  );
      }

      public bool TrySerialize(INetworkMessage message, int protocolVersion, IBufferWriter<byte> output, out int serializedLength)
      {
         if (this.serializers.TryGetValue(message.Command, out INetworkMessageSerializer? serializer))
         {
            serializedLength = serializer.Serialize(message, protocolVersion, output);
            return true;
         }

         serializedLength = 0;
         return false;
      }

      public bool TryDeserialize(string commandName, ref ReadOnlySequence<byte> data, int protocolVersion, [MaybeNullWhen(true)]out INetworkMessage message)
      {
         if (this.serializers.TryGetValue(commandName, out INetworkMessageSerializer? serializer))
         {
            message = serializer.Deserialize(ref data, protocolVersion);
            return true;
         }

         message = null!;
         return false;
      }
   }
}

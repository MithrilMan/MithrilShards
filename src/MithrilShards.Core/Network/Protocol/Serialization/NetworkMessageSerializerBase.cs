using System;
using System.Buffers;
using System.Reflection;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   public abstract class NetworkMessageSerializerBase<TMessageType> : INetworkMessageSerializer where TMessageType : INetworkMessage, new()
   {
      protected readonly INetworkDefinition chainDefinition;

      public NetworkMessageSerializerBase(INetworkDefinition chainDefinition)
      {
         this.chainDefinition = chainDefinition ?? throw new ArgumentNullException(nameof(chainDefinition));

         Type messageType = typeof(TMessageType);
         NetworkMessageAttribute? networkMessageAttribute = messageType.GetCustomAttribute<NetworkMessageAttribute>();
         if (networkMessageAttribute == null)
         {
            throw new InvalidOperationException($"{messageType.Name} must be decorated with {nameof(NetworkMessageAttribute)} to specify the protocol command it represents.");
         }
      }

      public abstract TMessageType Deserialize(ref SequenceReader<byte> reader, int protocolVersion, IPeerContext peerContext);

      public abstract void Serialize(TMessageType message, int protocolVersion, IPeerContext peerContext, IBufferWriter<byte> output);

      public Type GetMessageType()
      {
         return typeof(TMessageType);
      }

      public void Serialize(INetworkMessage message, int protocolVersion, IPeerContext peerContext, IBufferWriter<byte> output)
      {
         if (message is null)
         {
            throw new ArgumentNullException(nameof(message));
         }

         this.Serialize((TMessageType)message, protocolVersion, peerContext, output);
      }

      public INetworkMessage Deserialize(ref ReadOnlySequence<byte> data, int protocolVersion, IPeerContext peerContext)
      {
         var reader = new SequenceReader<byte>(data);
         return this.Deserialize(ref reader, protocolVersion, peerContext);
      }
   }
}

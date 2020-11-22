using System;
using System.Buffers;
using System.Reflection;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   public abstract class NetworkMessageSerializerBase<TMessageType, TPeerContext> : INetworkMessageSerializer
      where TMessageType : INetworkMessage, new()
      where TPeerContext : IPeerContext
   {

      public NetworkMessageSerializerBase()
      {
         Type messageType = typeof(TMessageType);
         NetworkMessageAttribute? networkMessageAttribute = messageType.GetCustomAttribute<NetworkMessageAttribute>();
         if (networkMessageAttribute == null)
         {
            throw new InvalidOperationException($"{messageType.Name} must be decorated with {nameof(NetworkMessageAttribute)} to specify the protocol command it represents.");
         }
      }

      public abstract TMessageType Deserialize(ref SequenceReader<byte> reader, int protocolVersion, TPeerContext peerContext);

      public abstract void Serialize(TMessageType message, int protocolVersion, TPeerContext peerContext, IBufferWriter<byte> output);

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

         Serialize((TMessageType)message, protocolVersion, (TPeerContext)peerContext, output);
      }

      public INetworkMessage Deserialize(ref ReadOnlySequence<byte> data, int protocolVersion, IPeerContext peerContext)
      {
         var reader = new SequenceReader<byte>(data);
         return Deserialize(ref reader, protocolVersion, (TPeerContext)peerContext);
      }
   }
}

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace MithrilShards.Core.Network.Protocol.Serialization {
   public abstract class NetworkMessageSerializerBase<TMessageType> : INetworkMessageSerializer where TMessageType : INetworkMessage {
      readonly IChainDefinition chainDefinition;

      public NetworkMessageSerializerBase(IChainDefinition chainDefinition) {
         this.chainDefinition = chainDefinition;
      }

      public abstract INetworkMessage Deserialize(ReadOnlySequence<byte> data, int protocolVersion);

      public abstract byte[] Serialize(TMessageType message, int protocolVersion, IBufferWriter<byte> output);

      public Type GetMessageType() {
         return typeof(TMessageType);
      }

      public byte[] Serialize(INetworkMessage message, int protocolVersion, IBufferWriter<byte> output) {
         Span<byte> outputSpan = output.GetSpan(4);

         this.Serialize((TMessageType)message, protocolVersion, output);

         // magic
         Span<byte> outputSpan = output.GetSpan(4);
         BinaryPrimitives.TryWriteUInt32LittleEndian(outputSpan, this.chainDefinition.Magic);
         output.Advance(4);

         // command
         outputSpan = output.GetSpan(12);
         ReadOnlySpan<char> commandSpan = message.Command.PadRight(12, '\0').AsSpan();
         Encoding.ASCII.GetBytes(commandSpan, outputSpan);
         output.Advance(12);
         
         // length
         outputSpan = output.GetSpan(4);
         BinaryPrimitives.TryWriteUInt32LittleEndian(outputSpan, this.chainDefinition.Magic);
         output.Advance(4);

         output.Write(message.Payload);

         return null;
      }
   }
}

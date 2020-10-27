using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Reflection;
using MithrilShards.Core.Crypto;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   public abstract class NetworkMessageSerializerBase<TMessageType> : INetworkMessageSerializer where TMessageType : INetworkMessage, new()
   {
      const int SIZE_MAGIC = 4;
      const int SIZE_COMMAND = 12;
      const int SIZE_PAYLOAD_LENGTH = 4;
      const int SIZE_CHECKSUM = 4;
      const int HEADER_LENGTH = SIZE_MAGIC + SIZE_COMMAND + SIZE_PAYLOAD_LENGTH + SIZE_CHECKSUM;

      private readonly byte[] precookedMagciAndCommand;

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

         #region build precooked Magic and Command header part
         // this block get executed only once because the serializer is singleton
         this.precookedMagciAndCommand = new byte[SIZE_MAGIC + SIZE_COMMAND];
         this.chainDefinition.MagicBytes.CopyTo(this.precookedMagciAndCommand, 0);

         //read the command name from the NetworkMessageAttribute
         var commandSpan = new Span<byte>(this.precookedMagciAndCommand, SIZE_MAGIC, SIZE_COMMAND);
         System.Text.Encoding.ASCII.GetBytes(networkMessageAttribute.Command, commandSpan);
         #endregion
      }

      public abstract TMessageType Deserialize(ref SequenceReader<byte> reader, int protocolVersion, IPeerContext peerContext);

      public abstract void Serialize(TMessageType message, int protocolVersion, IPeerContext peerContext, IBufferWriter<byte> output);

      public Type GetMessageType()
      {
         return typeof(TMessageType);
      }

      public int Serialize(INetworkMessage message, int protocolVersion, IPeerContext peerContext, IBufferWriter<byte> output)
      {
         if (message is null)
         {
            throw new ArgumentNullException(nameof(message));
         }

         var payloadOutput = new ArrayBufferWriter<byte>();
         this.Serialize((TMessageType)message, protocolVersion, peerContext, payloadOutput);

         output.Write(this.precookedMagciAndCommand);

         // length
         BinaryPrimitives.TryWriteUInt32LittleEndian(output.GetSpan(SIZE_PAYLOAD_LENGTH), (uint)payloadOutput.WrittenCount);
         output.Advance(SIZE_PAYLOAD_LENGTH);

         //checksum
         output.Write(HashGenerator.DoubleSha256(payloadOutput.WrittenSpan).Slice(0, 4));

         // payload
         output.Write(payloadOutput.WrittenSpan);

         return HEADER_LENGTH + payloadOutput.WrittenCount;
      }

      public INetworkMessage Deserialize(ref ReadOnlySequence<byte> data, int protocolVersion, IPeerContext peerContext)
      {
         var reader = new SequenceReader<byte>(data);
         return this.Deserialize(ref reader, protocolVersion, peerContext);
      }
   }
}

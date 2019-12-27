using Bedrock.Framework.Protocols;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using System;
using System.Buffers;

namespace MithrilShards.Network.Bedrock {
   /// <summary>
   /// Class to handle the common bitcoin-like protocol.
   /// Bitcoin protocol is composed of these fields
   /// Size |Description|Data Type  | Comments
   /// 4    | magic     |uint32_t   | Magic value indicating message origin network, and used to seek to next message when stream state is unknown
   /// 12   | command   |char[12]   | ASCII string identifying the packet content, NULL padded (non-NULL padding results in packet rejected)
   /// 4    | command   |uint32_t   | Length of payload in number of bytes
   /// 4    | command   |uint32_t   | First 4 bytes of sha256(sha256(payload))
   /// ?    | command   |uchar[]    | The actual data
   /// </summary>
   public class NetworkMessageProtocol : IProtocolReader<INetworkMessage>, IProtocolWriter<INetworkMessage> {
      readonly ILogger<NetworkMessageProtocol> logger;
      private readonly IChainDefinition chainDefinition;
      readonly INetworkMessageSerializerManager networkMessageSerializerManager;
      readonly ConnectionContextData contextData;
      private IPeerContext peerContext;

      public NetworkMessageProtocol(ILogger<NetworkMessageProtocol> logger,
                                    IChainDefinition chainDefinition,
                                    INetworkMessageSerializerManager networkMessageSerializerManager,
                                    ConnectionContextData contextData) {
         this.logger = logger;
         this.chainDefinition = chainDefinition;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.contextData = contextData;
      }

      internal void SetPeerContext(IPeerContext peerContext) {
         this.peerContext = peerContext;
      }

      public bool TryParseMessage(in ReadOnlySequence<byte> input, out SequencePosition consumed, out SequencePosition examined, out INetworkMessage message) {
         var reader = new SequenceReader<byte>(input);

         if (this.TryReadHeader(ref reader)) {
            // now try to read the payload
            if (reader.Remaining >= this.contextData.PayloadLength) {
               ReadOnlySequence<byte> payload = input.Slice(reader.Position, this.contextData.PayloadLength);
               //we consumed and examined everything, no matter if the message was a known message or not
               examined = consumed = payload.End;
               this.contextData.ResetFlags();

               string commandName = this.contextData.GetCommandName();

               if (this.networkMessageSerializerManager.Serializers.TryGetValue(commandName, out INetworkMessageSerializer serializer)) {
                  message = serializer.Deserialize(payload, this.peerContext.NegotiatedProtocolVersion.Version);
                  return true;
               }
               else {
                  this.logger.LogWarning("Serializer for message '{Command}' not found.", commandName);
                  message = new UnknownMessage(commandName, payload.ToArray());
                  return true;
               }
            }
         }

         // not enough data do read the full payload, so mark as examined the whole reader but let consumed just consume the expected payload length.
         consumed = reader.Position;
         examined = input.End;
         message = default;
         return false;
      }

      private bool TryReadHeader(ref SequenceReader<byte> reader) {
         if (!this.contextData.MagicNumberRead) {
            if (!this.TryReadMagicNumber(ref reader)) {
               return false;
            }
         }

         if (!this.contextData.CommandRead) {
            if (!this.TryReadCommand(ref reader)) {
               return false;
            }
         }

         if (!this.contextData.PayloadLengthRead) {
            if (!this.TryReadPayloadLenght(ref reader)) {
               return false;
            }
         }

         if (!this.contextData.ChecksumRead) {
            if (!this.TryReadChecksum(ref reader)) {
               return false;
            }
         }

         return true;
      }

      /// <summary>
      /// Tries to read the magic number from the buffer.
      /// It keep advancing till it has been found or end of buffer is reached.
      /// <paramref name="reader"/> advance up to the read magic number or, if not found it goes further trying to find a subset of it.
      /// In case a partial magic number has been found before reaching the end of the buffer, reader rewinds to the position of the first
      /// byte of the magic number that has been found.
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <remarks>When returns false, reader may be not to the end of the buffer in case a partial magic number lies at the end of the buffer.</remarks>
      /// <returns>true if the magic number has been full read, false otherwise (not enough bytes to read)</returns>
      private bool TryReadMagicNumber(ref SequenceReader<byte> reader) {
         // advance to the first byte of the magic number.
         while (reader.TryAdvanceTo(this.contextData.FirstMagicNumberByte, advancePastDelimiter: false)) {
            //TODO: compare sequence of bytes instead of reading an int
            if (reader.TryReadLittleEndian(out int magicRead)) {
               if (magicRead == this.contextData.MagicNumber) {
                  this.contextData.MagicNumberRead = true;
                  this.logger.LogDebug("Magic Number found, after skipping {SkippedBytes} bytes.", reader.Position.GetInteger() - 4);
                  return true;
               }
               else {
                  reader.Rewind(3);
               }
            }
            else {
               return false;
            }
         }

         // didn't found the first magic byte so can advance up to the end
         reader.Advance(reader.Remaining);
         return false;
      }

      /// <summary>
      /// Tries to read the command from the buffer fetching the next SIZE_COMMAND bytes.
      /// <paramref name="reader"/> doesn't advance if failing, otherwise it advance by <see cref="SIZE_COMMAND"/>
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <returns>true if the command has been read, false otherwise (not enough bytes to read)</returns>
      private bool TryReadCommand(ref SequenceReader<byte> reader) {
         if (reader.Remaining >= ConnectionContextData.SIZE_COMMAND) {
            ReadOnlySequence<byte> commandReader = reader.Sequence.Slice(reader.Position, ConnectionContextData.SIZE_COMMAND);
            this.contextData.Command = commandReader.ToArray();
            reader.Advance(ConnectionContextData.SIZE_COMMAND);
            return true;
         }
         else {
            return false;
         }
      }

      /// <summary>
      /// Tries to read the payload length from the buffer.
      /// <paramref name="reader"/> doesn't advance if failing, otherwise it advance by <see cref="SIZE_PAYLOAD_LENGTH"/>
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <returns>true if the payload length has been read, false otherwise (not enough bytes to read)</returns>
      private bool TryReadPayloadLenght(ref SequenceReader<byte> reader) {
         if (reader.TryReadLittleEndian(out int payloadLengthBytes)) {
            this.contextData.PayloadLength = (uint)payloadLengthBytes;
            return true;
         }
         else {
            return false;
         }
      }

      /// <summary>
      /// Tries to read the checksum from the buffer.
      /// <paramref name="reader"/> doesn't advance if failing, otherwise it advance by <see cref="SIZE_CHECKSUM"/>
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <returns>true if the checksum has been read, false otherwise (not enough bytes to read)</returns>
      private bool TryReadChecksum(ref SequenceReader<byte> reader) {
         if (reader.TryReadLittleEndian(out int checksumBytes)) {
            this.contextData.Checksum = (uint)checksumBytes;
            return true;
         }
         else {
            return false;
         }
      }


      public void WriteMessage(INetworkMessage message, IBufferWriter<byte> output) {
         if (message is null) {
            throw new ArgumentNullException(nameof(message));
         }

         if (this.networkMessageSerializerManager.Serializers.TryGetValue(message.Command, out INetworkMessageSerializer serializer)) {
            serializer.Serialize(message, this.peerContext.NegotiatedProtocolVersion.Version, output);
         }
         else {
            this.logger.LogWarning("Serializer for message '{Command}' not found.", message.Command);
         }
         //Span<byte> lengthBuffer = output.GetSpan(4);
         //BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, message.Payload.Length);
         //output.Advance(4);
         //output.Write(message.Payload);
      }
   }
}
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Crypto;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Network.Bedrock;

namespace MithrilShards.Chain.Bitcoin.Network.Bedrock
{
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
   public class BitcoinNetworkProtocolMessageSerializer : INetworkProtocolMessageSerializer
   {
      readonly ILogger<BitcoinNetworkProtocolMessageSerializer> _logger;
      private readonly INetworkDefinition _chainDefinition;
      readonly INetworkMessageSerializerManager _networkMessageSerializerManager;
      /// <summary>
      /// The deserialization context used to keep track of the ongoing deserialization of a stream.
      /// </summary>
      readonly DeserializationContext _deserializationContext;
      private IPeerContext _peerContext;

      public BitcoinNetworkProtocolMessageSerializer(ILogger<BitcoinNetworkProtocolMessageSerializer> logger, INetworkDefinition chainDefinition, INetworkMessageSerializerManager networkMessageSerializerManager)
      {
         _logger = logger;
         _chainDefinition = chainDefinition;
         _networkMessageSerializerManager = networkMessageSerializerManager;
         _deserializationContext = new DeserializationContext(chainDefinition.MagicBytes);

         _peerContext = null!; //initialized by SetPeerContext
      }

      public void SetPeerContext(IPeerContext peerContext)
      {
         _peerContext = peerContext;
      }

      public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, /*[MaybeNullWhen(false)]*/ out INetworkMessage message)
      {
         var reader = new SequenceReader<byte>(input);

         if (TryReadHeader(ref reader))
         {
            // now try to read the payload
            if (reader.Remaining >= _deserializationContext.PayloadLength)
            {
               ReadOnlySequence<byte> payload = input.Slice(reader.Position, _deserializationContext.PayloadLength);

               //check checksum
               ReadOnlySpan<byte> checksum = HashGenerator.DoubleSha256(payload.ToArray()).Slice(0, 4);
               if (_deserializationContext.Checksum != BitConverter.ToUInt32(checksum))
               {
                  throw new ProtocolViolationException("Invalid checksum.");
               }

               //we consumed and examined everything, no matter if the message was a known message or not
               examined = consumed = payload.End;
               _deserializationContext.ResetFlags();

               string commandName = _deserializationContext.CommandName!;
               if (_networkMessageSerializerManager
                  .TryDeserialize(commandName, ref payload, _peerContext.NegotiatedProtocolVersion.Version, _peerContext, out message!))
               {
                  int payloadSize = _deserializationContext.GetTotalMessageLength();
                  _logger.LogTrace("Received message '{Command}' with payload size {PayloadSize}.", commandName, payloadSize);
                  _peerContext.Metrics.Received(payloadSize);
                  return true;
               }
               else
               {
                  _logger.LogDebug("Serializer for message '{Command}' not found.", commandName);
                  message = new UnknownMessage(commandName, payload.ToArray());
                  _peerContext.Metrics.Wasted(_deserializationContext.GetTotalMessageLength());
                  return true;
               }
            }
         }

         // not enough data do read the full payload, so mark as examined the whole reader but let consumed just consume the expected payload length.
         consumed = reader.Position;
         examined = input.End;
         message = default!;
         return false;
      }

      private bool TryReadHeader(ref SequenceReader<byte> reader)
      {
         if (!_deserializationContext.MagicNumberRead)
         {
            if (!TryReadMagicNumber(ref reader))
            {
               return false;
            }
         }

         if (!_deserializationContext.CommandRead)
         {
            if (!TryReadCommand(ref reader))
            {
               return false;
            }
         }

         if (!_deserializationContext.PayloadLengthRead)
         {
            if (!TryReadPayloadLenght(ref reader))
            {
               return false;
            }
         }

         if (!_deserializationContext.ChecksumRead)
         {
            if (!TryReadChecksum(ref reader))
            {
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
      private bool TryReadMagicNumber(ref SequenceReader<byte> reader)
      {
         long prevRemaining = reader.Remaining;
         // advance to the first byte of the magic number.
         while (reader.TryAdvanceTo(_deserializationContext.FirstMagicNumberByte, advancePastDelimiter: false))
         {
            //TODO: compare sequence of bytes instead of reading an int
            if (reader.TryReadLittleEndian(out int magicRead))
            {
               if (magicRead == _deserializationContext.MagicNumber)
               {
                  _deserializationContext.MagicNumberRead = true;
                  return true;
               }
               else
               {
                  reader.Rewind(3);
                  //TODO check this logic
                  _peerContext.Metrics.Wasted(reader.Remaining - prevRemaining);
                  return false;
               }
            }
            else
            {
               _peerContext.Metrics.Wasted(reader.Remaining - prevRemaining);
               return false;
            }
         }

         // didn't found the first magic byte so can advance up to the end
         reader.Advance(reader.Remaining);
         _peerContext.Metrics.Wasted(reader.Remaining);
         return false;
      }

      /// <summary>
      /// Tries to read the command from the buffer fetching the next SIZE_COMMAND bytes.
      /// <paramref name="reader"/> doesn't advance if failing, otherwise it advance by <see cref="ProtocolDefinition.SIZE_COMMAND"/>
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <returns>true if the command has been read, false otherwise (not enough bytes to read)</returns>
      private bool TryReadCommand(ref SequenceReader<byte> reader)
      {
         if (reader.Remaining >= ProtocolDefinition.SIZE_COMMAND)
         {
            ReadOnlySequence<byte> commandReader = reader.Sequence.Slice(reader.Position, ProtocolDefinition.SIZE_COMMAND);
            _deserializationContext.SetCommand(ref commandReader);
            reader.Advance(ProtocolDefinition.SIZE_COMMAND);
            return true;
         }
         else
         {
            return false;
         }
      }

      /// <summary>
      /// Tries to read the payload length from the buffer.
      /// <paramref name="reader"/> doesn't advance if failing, otherwise it advance by <see cref="ProtocolDefinition.SIZE_PAYLOAD_LENGTH"/>
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <returns>true if the payload length has been read, false otherwise (not enough bytes to read)</returns>
      private bool TryReadPayloadLenght(ref SequenceReader<byte> reader)
      {
         if (reader.TryReadLittleEndian(out int payloadLengthBytes))
         {
            _deserializationContext.PayloadLength = (uint)payloadLengthBytes;
            return true;
         }
         else
         {
            return false;
         }
      }

      /// <summary>
      /// Tries to read the checksum from the buffer.
      /// <paramref name="reader"/> doesn't advance if failing, otherwise it advance by <see cref="ProtocolDefinition.SIZE_CHECKSUM"/>
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <returns>true if the checksum has been read, false otherwise (not enough bytes to read)</returns>
      private bool TryReadChecksum(ref SequenceReader<byte> reader)
      {
         if (reader.TryReadLittleEndian(out int checksumBytes))
         {
            _deserializationContext.Checksum = (uint)checksumBytes;
            return true;
         }
         else
         {
            return false;
         }
      }


      public void WriteMessage(INetworkMessage message, IBufferWriter<byte> output)
      {
         if (message is null)
         {
            throw new ArgumentNullException(nameof(message));
         }

         string command = message.Command;
         using (_logger.BeginScope("Serializing and sending '{Command}'", command))
         {
            var payloadOutput = new ArrayBufferWriter<byte>();
            if (_networkMessageSerializerManager.TrySerialize(message,
                                                                  _peerContext.NegotiatedProtocolVersion.Version,
                                                                  _peerContext,
                                                                  payloadOutput))
            {
               int payloadSize = payloadOutput.WrittenCount;

               // write magic bytes (it's expected to be SIZE_MAGIC bytes long)
               _chainDefinition.MagicBytes.CopyTo(output.GetSpan(ProtocolDefinition.SIZE_MAGIC));
               output.Advance(ProtocolDefinition.SIZE_MAGIC);

               // write command name
               Span<byte> commandSpan = stackalloc byte[ProtocolDefinition.SIZE_COMMAND];
               System.Text.Encoding.ASCII.GetBytes(command, commandSpan);
               output.Write(commandSpan);

               // write payload length
               BinaryPrimitives.TryWriteUInt32LittleEndian(output.GetSpan(ProtocolDefinition.SIZE_PAYLOAD_LENGTH), (uint)payloadSize);
               output.Advance(ProtocolDefinition.SIZE_PAYLOAD_LENGTH);

               // write payload checksum
               output.Write(HashGenerator.DoubleSha256(payloadOutput.WrittenSpan).Slice(0, ProtocolDefinition.SIZE_CHECKSUM));

               // write payload
               output.Write(payloadOutput.WrittenSpan);

               _peerContext.Metrics.Sent(ProtocolDefinition.HEADER_LENGTH + payloadSize);
               _logger.LogTrace("Sent message '{Command}' with payload size {PayloadSize}.", command, payloadSize);
            }
            else
            {
               _logger.LogDebug("Serializer for message '{Command}' not found.", command);
            }
         }
      }
   }
}
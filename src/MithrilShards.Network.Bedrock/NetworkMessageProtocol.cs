using Bedrock.Framework.Protocols;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Network.Protocol;
using System;
using System.Buffers;
using System.Buffers.Binary;

namespace MithrilShards.P2P.Bedrock {
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
   public class NetworkMessageProtocol : IProtocolReader<Message>, IProtocolWriter<Message> {
      private const long SIZE_MAGIC = 4;
      private const long SIZE_COMMAND = 12;
      private const long SIZE_PAYLOAD_LENGTH = 4;
      private const long SIZE_CHECKSUM = 4;

      readonly ILogger<NetworkMessageProtocol> logger;
      private readonly IChainDefinition chainDefinition;
      private readonly byte[] magicNumberBytes;
      private readonly int magicNumber;

      private bool magicNumberRead = false;
      private byte[] command;
      private bool commandRead = false;
      private uint payloadLength;
      private bool payloadLengthRead = false;
      private uint checksum;
      private bool checksumRead = false;


      public NetworkMessageProtocol(ILogger<NetworkMessageProtocol> logger, IChainDefinition chainDefinition) {
         this.logger = logger;
         this.chainDefinition = chainDefinition;

         this.magicNumberBytes = this.chainDefinition.MagicBytes;
         this.magicNumber = BitConverter.ToInt32(this.chainDefinition.MagicBytes);
      }


      public bool TryParseMessage(in ReadOnlySequence<byte> input, out SequencePosition consumed, out SequencePosition examined, out Message message) {
         var reader = new SequenceReader<byte>(input);

         if (!this.magicNumberRead) {
            this.magicNumberRead = this.TryReadMagicNumber(ref reader);
            if (!this.magicNumberRead) {
               consumed = reader.Position;
               examined = input.End;
               message = default;
               return false;
            }
         }

         if (!this.commandRead) {
            this.commandRead = this.TryReadCommand(ref reader);
            if (!this.commandRead) {
               consumed = reader.Position;
               examined = input.End;
               message = default;
               return false;
            }
         }

         if (!this.payloadLengthRead) {
            this.payloadLengthRead = this.TryReadPayloadLenght(ref reader);
            if (!this.payloadLengthRead) {
               consumed = reader.Position;
               examined = input.End;
               message = default;
               return false;
            }
         }

         if (!this.checksumRead) {
            this.checksumRead = this.TryReadChecksum(ref reader);
            if (!this.checksumRead) {
               consumed = reader.Position;
               examined = input.End;
               message = default;
               return false;
            }
         }


         // now try to read the payload
         if (reader.Remaining >= this.payloadLength) {
            ReadOnlySequence<byte> payload = input.Slice(reader.Position, this.payloadLength);
            message = new Message(this.chainDefinition.MagicBytes, this.command, this.payloadLength, this.checksum, payload.ToArray());

            examined = consumed = payload.End;
            this.magicNumberRead = this.commandRead = this.payloadLengthRead = this.checksumRead = false;
            return true;
         }
         else {
            // not enough data do read the full payload, so mark as examined the whole reader but let consumed just consume the expected payload length.
            consumed = reader.Position;
            examined = input.End;
            message = default;
            return false;
         }
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
         while (reader.TryAdvanceTo(this.magicNumberBytes[0], advancePastDelimiter: false)) {
            if (reader.TryReadLittleEndian(out int magicRead)) {
               if (magicRead == this.magicNumber) {
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
         if (reader.Remaining >= SIZE_COMMAND) {
            ReadOnlySequence<byte> commandReader = reader.Sequence.Slice(reader.Position, SIZE_COMMAND);
            this.command = commandReader.ToArray();
            reader.Advance(SIZE_COMMAND);
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
            this.payloadLength = (uint)payloadLengthBytes;
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
            this.checksum = (uint)checksumBytes;
            return true;
         }
         else {
            return false;
         }
      }


      public void WriteMessage(Message message, IBufferWriter<byte> output) {
         Span<byte> lengthBuffer = output.GetSpan(4);
         BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, message.Payload.Length);
         output.Advance(4);
         output.Write(message.Payload);
      }
   }

   public struct Message {
      private readonly byte[] magic;
      private readonly byte[] command;
      private readonly uint payloadLength;
      private readonly uint checksum;
      private readonly byte[] payload;

      public ReadOnlySpan<byte> Magic => this.magic;

      public ReadOnlySpan<byte> Command => this.command;

      public uint PayloadLength => this.payloadLength;

      public uint Checksum => this.checksum;

      public ReadOnlySpan<byte> Payload => this.payload;

      public Message(byte[] magic, byte[] command, uint payloadLength, uint checksum, byte[] payload) {
         this.magic = magic;
         this.command = command;
         this.payloadLength = payloadLength;
         this.checksum = checksum;
         this.payload = payload;
      }
   }
}

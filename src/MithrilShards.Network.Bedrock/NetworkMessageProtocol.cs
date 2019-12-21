using Bedrock.Framework.Protocols;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Network.Protocol;
using System;
using System.Buffers;
using System.Buffers.Binary;

namespace MithrilShards.P2P.Bedrock {
   public class NetworkMessageProtocol : IProtocolReader<Message>, IProtocolWriter<Message> {
      private const long COMMAND_LENGTH = 12;

      bool magicNumberRead = false;
      private byte messageLengthExpected;
      readonly ILogger<NetworkMessageProtocol> logger;
      private readonly IChainDefinition chainDefinition;
      private readonly byte[] magicNumberBytes;
      private readonly int magicNumber;

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

         // try to read the command name
         if (reader.Remaining >= COMMAND_LENGTH) {

            //reader.TryRead()
         }
         else {
            consumed = examined = reader.Position;
            message = default;
            return false;
         }

         // try to read the message length
         if (reader.TryRead(out byte payloadLength)) {
            if (reader.Remaining >= payloadLength) {
               ReadOnlySequence<byte> payload = input.Slice(reader.Position, payloadLength);
               message = new Message(this.chainDefinition.MagicBytes, payload.ToArray());

               examined = consumed = payload.End;
               this.magicNumberRead = false; //message sent, so we can reset magicNumber flag
               return true;
            }
            else {
               this.messageLengthExpected = payloadLength;
               // not enough data do read the full payload, so mark as examined the whole reader but let consumed just consume the expected payload length.
               message = default;
               examined = input.End;
               consumed = reader.Position;
               return false;
            }
         }
         else {
            //no data to read, so current reader position is at the end of the buffer and we consumed it all;
            message = default;
            examined = consumed = reader.Position;
            return false;
         }

      }

      private bool TryReadMagicNumber(ref SequenceReader<byte> reader) {
         // advance to the first byte of the magic number.
         while (reader.TryAdvanceTo(this.magicNumberBytes[0], advancePastDelimiter: false)) {
            if (reader.TryReadLittleEndian(out int magicRead)) {
               if (magicRead == this.magicNumber) {
                  //this.logger.LogDebug("Magic Number found, after skipping {SkippedBytes} bytes.", reader.Position.GetInteger() - 4);

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

      private bool TryReadMagicNumberOld(ref SequenceReader<byte> reader) {
         for (int i = 0; i < this.magicNumberBytes.Length; i++) {
            byte expectedByte = this.magicNumberBytes[i];

            if (reader.TryRead(out byte receivedByte)) {
               if (expectedByte != receivedByte) {
                  // If we did not receive the next byte we expected
                  // we either received the first byte of the magic value
                  // or not. If yes, we set index to 0 here, which is then
                  // incremented in for loop to 1 and we thus continue
                  // with the second byte. Otherwise, we set index to -1
                  // here, which means that after the loop incrementation,
                  // we will start from first byte of magic.
                  i = receivedByte == this.magicNumberBytes[0] ? 0 : -1;
               }
            }
            else {
               //nothing left to read
               // in case there are partial matches for the magic packet, don't consider them as consumed
               // so they will be examined again next iteration when hopefully the full magic number will be present
               reader.Rewind(i);

               return false;
            }
         }
         return true;
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
      private readonly byte[] payload;

      public Message(byte[] magic, byte[] payload) {
         this.magic = magic;
         this.payload = payload;
      }

      public ReadOnlySpan<byte> Payload => this.payload;
   }
}

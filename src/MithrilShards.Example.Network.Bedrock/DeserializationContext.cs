using System;
using System.Buffers;
using System.Net;
using System.Text;

namespace MithrilShards.Example.Network.Bedrock
{
   public class DeserializationContext
   {
      private uint _payloadLength;
      private uint _checksum;

      public bool ChecksumRead { get; private set; }
      public byte[] MagicNumberBytes { get; }

      public int MagicNumber { get; }

      public bool MagicNumberRead { get; set; }

      public bool PayloadLengthRead { get; private set; }

      public bool CommandRead { get; private set; }

      public string CommandName { get; private set; } = default!;

      public readonly byte FirstMagicNumberByte;

      /// <summary>
      /// The maximum allowed protocol message length.
      /// </summary>
      private readonly uint _maximumProtocolMessageLength;

      /// <summary>
      /// Gets or sets the length of the last parsed INetworkMessage payload (message length - header length).
      /// Sets PayloadRead to true.
      /// </summary>
      /// <value>
      /// The length of the payload.
      /// </value>
      public uint PayloadLength
      {
         get => this._payloadLength;
         set
         {
            if (value > this._maximumProtocolMessageLength)
            {
               throw new ProtocolViolationException($"Message size exceeds the maximum value {this._maximumProtocolMessageLength}.");
            }
            this._payloadLength = value;
            this.PayloadLengthRead = true;
         }
      }

      /// <summary>
      /// Gets or sets the command that will instruct how to parse the INetworkMessage payload.
      /// Sets CommandRead to true.
      /// </summary>
      /// <value>
      /// The raw byte of command part of the message header (expected 12 chars right padded with '\0').
      /// </value>
      public uint Checksum
      {
         get => this._checksum;
         set
         {
            this._checksum = value;
            this.ChecksumRead = true;
         }
      }

      public DeserializationContext(byte[] magicNumberBytesmagicBytes, uint maximumProtocolMessageLength = ProtocolDefinition.DEFAULT_MAX_PROTOCOL_MESSAGE_LENGTH)
      {
         this.MagicNumberBytes = magicNumberBytesmagicBytes;
         this._maximumProtocolMessageLength = maximumProtocolMessageLength;
         this.MagicNumber = BitConverter.ToInt32(magicNumberBytesmagicBytes);
         this.FirstMagicNumberByte = magicNumberBytesmagicBytes[0];

         this.ResetFlags();
      }

      public void ResetFlags()
      {
         this.MagicNumberRead = false;
         this.PayloadLengthRead = false;
         this.CommandRead = false;
         this.ChecksumRead = false;
      }

      /// <summary>
      /// Gets or sets the command that will instruct how to parse the INetworkMessage payload.
      /// Sets CommandRead to true.
      /// </summary>
      /// <value>
      /// The raw byte of command part of the message header (expected 12 chars right padded with '\0').
      /// </value>
      public void SetCommand(ref ReadOnlySequence<byte> command)
      {
         this.CommandName = Encoding.ASCII.GetString((command.IsSingleSegment ? command.FirstSpan : command.ToArray()).Trim((byte)'\0'));
         this.CommandRead = true;
      }

      public int GetTotalMessageLength()
      {
         return ProtocolDefinition.HEADER_LENGTH + (int)this._payloadLength;
      }
   }
}

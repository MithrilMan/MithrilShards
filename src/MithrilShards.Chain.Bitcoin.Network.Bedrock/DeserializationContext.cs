using System;
using System.Buffers;
using System.Net;
using System.Text;

namespace MithrilShards.Chain.Bitcoin.Network.Bedrock;

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
      get => _payloadLength;
      set
      {
         if (value > _maximumProtocolMessageLength)
         {
            throw new ProtocolViolationException($"Message size exceeds the maximum value {_maximumProtocolMessageLength}.");
         }
         _payloadLength = value;
         PayloadLengthRead = true;
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
      get => _checksum;
      set
      {
         _checksum = value;
         ChecksumRead = true;
      }
   }

   public DeserializationContext(byte[] magicNumberBytesmagicBytes, uint maximumProtocolMessageLength = ProtocolDefinition.DEFAULT_MAX_PROTOCOL_MESSAGE_LENGTH)
   {
      MagicNumberBytes = magicNumberBytesmagicBytes;
      _maximumProtocolMessageLength = maximumProtocolMessageLength;
      MagicNumber = BitConverter.ToInt32(magicNumberBytesmagicBytes);
      FirstMagicNumberByte = magicNumberBytesmagicBytes[0];

      ResetFlags();
   }

   public void ResetFlags()
   {
      MagicNumberRead = false;
      PayloadLengthRead = false;
      CommandRead = false;
      ChecksumRead = false;
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
      CommandName = Encoding.ASCII.GetString((command.IsSingleSegment ? command.FirstSpan : command.ToArray()).Trim((byte)'\0'));
      CommandRead = true;
   }

   public int GetTotalMessageLength()
   {
      return ProtocolDefinition.HEADER_LENGTH + (int)_payloadLength;
   }
}

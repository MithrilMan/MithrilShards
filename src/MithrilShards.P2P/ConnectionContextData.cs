using System;
using System.Text;

namespace MithrilShards.Network.Legacy
{
   public class ConnectionContextData
   {
      public const int SIZE_MAGIC = 4;
      public const int SIZE_COMMAND = 12;
      public const int SIZE_PAYLOAD_LENGTH = 4;
      public const int SIZE_CHECKSUM = 4;
      public const int HEADER_LENGTH = SIZE_MAGIC + SIZE_COMMAND + SIZE_PAYLOAD_LENGTH + SIZE_CHECKSUM;

      private uint payloadLength;
      private byte[]? command;
      private uint checksum;

      public bool ChecksumRead { get; private set; }

      private readonly byte[] magicNumberBytes;

      public int MagicNumber { get; }

      public bool MagicNumberRead { get; set; }

      public bool PayloadLengthRead { get; private set; }

      public bool CommandRead { get; private set; }

      public readonly byte FirstMagicNumberByte;

      /// <summary>
      /// Gets or sets the length of the last parsed INetworkMessage payload (message length - header length).
      /// Sets PayloadRead to true.
      /// </summary>
      /// <value>
      /// The length of the payload.
      /// </value>
      public uint PayloadLength
      {
         get => this.payloadLength;
         set
         {
            this.payloadLength = value;
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
         get => this.checksum;
         set
         {
            this.checksum = value;
            this.ChecksumRead = true;
         }
      }

      public ConnectionContextData(byte[] magicNumberBytesmagicBytes)
      {
         this.magicNumberBytes = magicNumberBytesmagicBytes;
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
      /// Gets the command that will instruct how to parse the INetworkMessage payload.
      /// </summary>
      /// <returns>
      /// The raw byte of command part of the message header (expected 12 chars right padded with '\0').
      /// </returns>
      public byte[]? GetCommand() => this.command;

      /// <summary>
      /// Sets the command that will instruct how to parse the INetworkMessage payload.
      /// Sets CommandRead to true.
      /// </summary>
      /// <param name="value">
      /// The raw byte of command part of the message header (expected 12 chars right padded with '\0').
      /// </param>
      public void SetCommand(byte[] value)
      {
         this.command = value;
         this.CommandRead = true;
      }

      public byte[] GetMagicNumberBytes() => this.magicNumberBytes;

      public string GetCommandName()
      {
         return Encoding.ASCII.GetString(this.command.AsSpan().Trim((byte)'\0'));
      }

      public int GetTotalMessageLength()
      {
         return HEADER_LENGTH + (int)this.payloadLength;
      }
   }
}

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization {
   public static class SequenceReaderExtensions {
      private const string NotEnoughBytesLeft = "Cannot read data, not enough bytes left.";
      public static bool ReadBool(ref this SequenceReader<byte> reader, bool isBigEndian = false) {
         return reader.TryRead(out byte value) ? (value > 0) : throw new MessageSerializationException(NotEnoughBytesLeft);
      }

      public static short ReadShort(ref this SequenceReader<byte> reader, bool isBigEndian = false) {
         if (isBigEndian) {
            return reader.TryReadBigEndian(out short value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return reader.TryReadLittleEndian(out short value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public static ushort ReadUShort(ref this SequenceReader<byte> reader, bool isBigEndian = false) {
         if (isBigEndian) {
            return reader.TryReadBigEndian(out short value) ? (ushort)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return reader.TryReadLittleEndian(out short value) ? (ushort)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public static int ReadInt(ref this SequenceReader<byte> reader, bool isBigEndian = false) {
         if (isBigEndian) {
            return reader.TryReadBigEndian(out int value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return reader.TryReadLittleEndian(out int value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public static uint ReadUInt(ref this SequenceReader<byte> reader, bool isBigEndian = false) {
         if (isBigEndian) {
            return reader.TryReadBigEndian(out int value) ? (uint)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return reader.TryReadLittleEndian(out int value) ? (uint)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public static long ReadLong(ref this SequenceReader<byte> reader, bool isBigEndian = false) {
         if (isBigEndian) {
            return reader.TryReadBigEndian(out long value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return reader.TryReadLittleEndian(out long value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public static ulong ReadULong(ref this SequenceReader<byte> reader, bool isBigEndian = false) {
         if (isBigEndian) {
            return reader.TryReadBigEndian(out long value) ? (ulong)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return reader.TryReadLittleEndian(out long value) ? (ulong)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public static string ReadVarString(ref this SequenceReader<byte> reader) {
         ulong stringLength = ReadVarInt(ref reader);
         ReadOnlySequence<byte> result = reader.Sequence.Slice(reader.Position, (int)stringLength);
         reader.Advance((long)stringLength);

         // in case the string lies in a single span we can save a copy to a byte array
         return Encoding.ASCII.GetString(result.IsSingleSegment ? result.FirstSpan : result.ToArray());
      }

      public static byte[] ReadBytes(ref this SequenceReader<byte> reader, int length) {
         ReadOnlySequence<byte> result = reader.Sequence.Slice(reader.Position, (int)length);
         reader.Advance((long)length);

         // in case the string lies in a single span we can save a copy to a byte array
         return result.ToArray();
      }

      public static ulong ReadVarInt(ref this SequenceReader<byte> reader) {
         reader.TryRead(out byte firstByte);
         if (firstByte < 0xFD) {
            return (ulong)firstByte;
         }
         else if (firstByte == 0xFD) {
            return (ulong)reader.ReadUShort();
         }
         else if (firstByte == 0xFE) {
            return (ulong)reader.ReadUInt();
         }
         // == 0xFF
         else {
            return (ulong)reader.ReadULong();
         }
      }

      /// <summary>
      /// Reads the network address.
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <param name="skipTimeField">if set to <c>true</c> skips time field serialization/deserialization, used by <see cref="VersionMessage"/>.</param>
      /// <returns></returns>
      public static NetworkAddress ReadNetworkAddress(ref this SequenceReader<byte> reader, bool skipTimeField) {
         var result = new NetworkAddress(skipTimeField);
         var innerReader = new SequenceReader<byte>(reader.Sequence.Slice(reader.Position, result.Length));
         result.Deserialize(innerReader);
         reader.Advance(innerReader.Consumed);
         return result;
      }
   }
}

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization
{
   public static class SequenceReaderExtensions
   {
      /// <summary>
      /// Deserialize a <typeparamref name="TItem"/> type.
      /// </summary>
      /// <typeparam name="TItem">The type of the item to deserialize.</typeparam>
      /// <param name="reader">The reader from which fetch data.</param>
      /// <returns>The serialized item.</returns>
      public delegate TItem ItemDeserializer<TItem>(ref SequenceReader<byte> reader);

      private const string NotEnoughBytesLeft = "Cannot read data, not enough bytes left.";

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool ReadBool(ref this SequenceReader<byte> reader)
      {
         return reader.TryRead(out byte value) ? (value > 0) : throw new MessageSerializationException(NotEnoughBytesLeft);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static byte ReadByte(ref this SequenceReader<byte> reader)
      {
         return reader.TryRead(out byte value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static short ReadShort(ref this SequenceReader<byte> reader, bool isBigEndian = false)
      {
         if (isBigEndian)
         {
            return reader.TryReadBigEndian(out short value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else
         {
            return reader.TryReadLittleEndian(out short value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static ushort ReadUShort(ref this SequenceReader<byte> reader, bool isBigEndian = false)
      {
         if (isBigEndian)
         {
            return reader.TryReadBigEndian(out short value) ? (ushort)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else
         {
            return reader.TryReadLittleEndian(out short value) ? (ushort)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int ReadInt(ref this SequenceReader<byte> reader, bool isBigEndian = false)
      {
         if (isBigEndian)
         {
            return reader.TryReadBigEndian(out int value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else
         {
            return reader.TryReadLittleEndian(out int value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static uint ReadUInt(ref this SequenceReader<byte> reader, bool isBigEndian = false)
      {
         if (isBigEndian)
         {
            return reader.TryReadBigEndian(out int value) ? (uint)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else
         {
            return reader.TryReadLittleEndian(out int value) ? (uint)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static long ReadLong(ref this SequenceReader<byte> reader, bool isBigEndian = false)
      {
         if (isBigEndian)
         {
            return reader.TryReadBigEndian(out long value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else
         {
            return reader.TryReadLittleEndian(out long value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static ulong ReadULong(ref this SequenceReader<byte> reader, bool isBigEndian = false)
      {
         if (isBigEndian)
         {
            return reader.TryReadBigEndian(out long value) ? (ulong)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else
         {
            return reader.TryReadLittleEndian(out long value) ? (ulong)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static string ReadVarString(ref this SequenceReader<byte> reader)
      {
         ulong stringLength = ReadVarInt(ref reader);
         ReadOnlySequence<byte> result = reader.Sequence.Slice(reader.Position, (int)stringLength);
         reader.Advance((long)stringLength);

         // in case the string lies in a single span we can save a copy to a byte array
         return Encoding.ASCII.GetString(result.IsSingleSegment ? result.FirstSpan : result.ToArray());
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static ReadOnlySpan<byte> ReadBytes(ref this SequenceReader<byte> reader, int length)
      {
         ReadOnlySequence<byte> sequence = reader.Sequence.Slice(reader.Position, length);
         reader.Advance(length);

         if (sequence.IsSingleSegment)
         {
            return sequence.FirstSpan;
         }
         else
         {
            return sequence.ToArray();
         }
      }

      /// <summary>
      /// Reads the byte array, reading first a VarInt of the size of the array, followed by the full array data.
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <param name="length">The length.</param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static byte[]? ReadByteArray(ref this SequenceReader<byte> reader)
      {
         int arraySize = (int)ReadVarInt(ref reader);

         if (arraySize == 0) return null;

         ReadOnlySequence<byte> sequence = reader.Sequence.Slice(reader.Position, arraySize);
         reader.Advance(arraySize);

         if (sequence.IsSingleSegment)
         {
            return sequence.FirstSpan.ToArray();
         }
         else
         {
            return sequence.ToArray();
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static ulong ReadVarInt(ref this SequenceReader<byte> reader)
      {
         reader.TryRead(out byte firstByte);
         if (firstByte < 0xFD)
         {
            return firstByte;
         }
         else if (firstByte == 0xFD)
         {
            return reader.ReadUShort();
         }
         else if (firstByte == 0xFE)
         {
            return reader.ReadUInt();
         }
         // == 0xFF
         else
         {
            return reader.ReadULong();
         }
      }

      /// <summary>
      /// Reads an array of <typeparamref name="TSerializableType" /> types.
      /// Internally it expects a VarInt that specifies the length of items to read.
      /// </summary>
      /// <typeparam name="TSerializableType">The type of the serializable type.</typeparam>
      /// <param name="reader">The reader.</param>
      /// <param name="protocolVersion">The protocol version.</param>
      /// <param name="serializer">The serializer.</param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static TSerializableType[] ReadArray<TSerializableType>(ref this SequenceReader<byte> reader, int protocolVersion, IProtocolTypeSerializer<TSerializableType> serializer, ProtocolTypeSerializerOptions? options = null)
      {
         ulong itemsCount = reader.ReadVarInt();

         TSerializableType[] result = new TSerializableType[itemsCount];

         for (ulong i = 0; i < itemsCount; i++)
         {
            result[i] = serializer.Deserialize(ref reader, protocolVersion, options);
         }

         return result;
      }

      /// <summary>
      /// Reads an item of <typeparamref name="TSerializableType" /> type using the passed typed serializer.
      /// </summary>
      /// <typeparam name="TSerializableType">The type of the serializable type.</typeparam>
      /// <param name="reader">The reader.</param>
      /// <param name="protocolVersion">The protocol version.</param>
      /// <param name="serializer">The serializer.</param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static TSerializableType ReadWithSerializer<TSerializableType>(ref this SequenceReader<byte> reader, int protocolVersion, IProtocolTypeSerializer<TSerializableType> serializer, ProtocolTypeSerializerOptions? options = null)
      {
         return serializer.Deserialize(ref reader, protocolVersion, options);
      }
   }
}
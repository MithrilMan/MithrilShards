﻿using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization
{
   public static class IBufferWriterExtensions
   {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteBool(this IBufferWriter<byte> writer, bool value)
      {
         const int size = 1;
         writer.GetSpan(size)[0] = (byte)(value ? 1 : 0);
         writer.Advance(size);
         return size;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteByte(this IBufferWriter<byte> writer, byte value)
      {
         const int size = 1;
         writer.GetSpan(size)[0] = value;
         writer.Advance(size);
         return 1;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteShort(this IBufferWriter<byte> writer, short value, bool isBigEndian = false)
      {
         const int size = 2;
         if (isBigEndian)
         {
            BinaryPrimitives.WriteInt16BigEndian(writer.GetSpan(size), value);
         }
         else
         {
            BinaryPrimitives.WriteInt16LittleEndian(writer.GetSpan(size), value);
         }

         writer.Advance(size);
         return size;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteUShort(this IBufferWriter<byte> writer, ushort value, bool isBigEndian = false)
      {
         const int size = 2;
         if (isBigEndian)
         {
            BinaryPrimitives.WriteUInt16BigEndian(writer.GetSpan(size), value);
         }
         else
         {
            BinaryPrimitives.WriteUInt16LittleEndian(writer.GetSpan(size), value);
         }

         writer.Advance(size);
         return size;
      }


      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteInt(this IBufferWriter<byte> writer, int value, bool isBigEndian = false)
      {
         const int size = 4;
         if (isBigEndian)
         {
            BinaryPrimitives.WriteInt32BigEndian(writer.GetSpan(size), value);
         }
         else
         {
            BinaryPrimitives.WriteInt32LittleEndian(writer.GetSpan(size), value);
         }

         writer.Advance(size);
         return size;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteUInt(this IBufferWriter<byte> writer, uint value, bool isBigEndian = false)
      {
         const int size = 4;
         if (isBigEndian)
         {
            BinaryPrimitives.WriteUInt32BigEndian(writer.GetSpan(size), value);
         }
         else
         {
            BinaryPrimitives.WriteUInt32LittleEndian(writer.GetSpan(size), value);
         }

         writer.Advance(size);
         return size;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteLong(this IBufferWriter<byte> writer, long value, bool isBigEndian = false)
      {
         const int size = 8;
         if (isBigEndian)
         {
            BinaryPrimitives.WriteInt64BigEndian(writer.GetSpan(size), value);
         }
         else
         {
            BinaryPrimitives.WriteInt64LittleEndian(writer.GetSpan(size), value);
         }

         writer.Advance(size);
         return size;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteULong(this IBufferWriter<byte> writer, ulong value, bool isBigEndian = false)
      {
         const int size = 8;
         if (isBigEndian)
         {
            BinaryPrimitives.WriteUInt64BigEndian(writer.GetSpan(size), value);
         }
         else
         {
            BinaryPrimitives.WriteUInt64LittleEndian(writer.GetSpan(size), value);
         }

         writer.Advance(size);
         return size;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteVarString(this IBufferWriter<byte> writer, string value)
      {
         int stringLength = value?.Length ?? 0;
         int size = WriteVarInt(writer, (ulong)stringLength);

         Encoding.ASCII.GetBytes(value.AsSpan(), writer.GetSpan(stringLength));
         writer.Advance(stringLength);
         return size + stringLength;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteBytes(this IBufferWriter<byte> writer, byte[] value)
      {
         if (value is null)
         {
            ThrowHelper.ThrowArgumentNullException(nameof(value));
         }

         writer.Write(value);
         return value.Length;
      }

      /// <summary>
      /// Writes the byte array writing first a VarInt of the size of the array, followed by the full array data.
      /// </summary>
      /// <param name="writer">The writer.</param>
      /// <param name="value">The value.</param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteByteArray(this IBufferWriter<byte> writer, byte[]? value)
      {
         ulong arraySize = (ulong)(value?.Length ?? 0);

         int size = writer.WriteVarInt(arraySize);

         if (arraySize > 0)
         {
            size += writer.WriteBytes(value!);
         }

         return size;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteVarInt(this IBufferWriter<byte> writer, ulong value)
      {
         if (value < 0xFD)
         {
            const int size = 1;
            writer.GetSpan(size)[0] = (byte)value;
            writer.Advance(size);
            return size;
         }
         else if (value <= 0xFFFF)
         {
            const int size = 3;
            Span<byte> span = writer.GetSpan(size);
            span[0] = 0xFD;
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(1, size - 1), (ushort)value);
            writer.Advance(size);
            return size;
         }
         else if (value == 0XFFFFFFFF)
         {
            const int size = 5;
            Span<byte> span = writer.GetSpan(size);
            span[0] = 0xFE;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(1, size - 1), (uint)value);
            writer.Advance(size);
            return size;
         }
         // == 0xFF
         else
         {
            const int size = 9;
            Span<byte> span = writer.GetSpan(size);
            span[0] = 0xFF;
            BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(1, size - 1), value);
            writer.Advance(size);
            return size;
         }
      }

      /// <summary>
      /// Writes the array of passed <typeparamref name="TSerializableType" /> types.
      /// Internally it writes a VarInt followed by the list of serialized items.
      /// </summary>
      /// <typeparam name="TSerializableType">The type of the serializable type.</typeparam>
      /// <param name="writer">The writer.</param>
      /// <param name="items">The items.</param>
      /// <param name="protocolVersion">The protocol version.</param>
      /// <param name="serializer">The serializer.</param>
      /// <param name="options"></param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteArray<TSerializableType>(this IBufferWriter<byte> writer, TSerializableType[]? items, int protocolVersion, IProtocolTypeSerializer<TSerializableType> serializer, ProtocolTypeSerializerOptions? options = null)
      {
         if ((items?.Length ?? 0) == 0)
         {
            return writer.WriteVarInt(0);
         }

         int size = WriteVarInt(writer, (ulong)items!.Length);

         for (int i = 0; i < items.Length; i++)
         {
            size += serializer.Serialize(items[i], protocolVersion, writer, options);
         }

         return size;
      }

      /// <summary>
      /// Writes the item of passed <typeparamref name="TSerializableType" /> type using the passed typed serializer.
      /// </summary>
      /// <typeparam name="TSerializableType">The type of the serializable type.</typeparam>
      /// <param name="writer">The writer.</param>
      /// <param name="item">The item to serialize.</param>
      /// <param name="protocolVersion">The protocol version.</param>
      /// <param name="serializer">The serializer.</param>
      /// <param name="options"></param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static int WriteWithSerializer<TSerializableType>(this IBufferWriter<byte> writer, TSerializableType item, int protocolVersion, IProtocolTypeSerializer<TSerializableType> serializer, ProtocolTypeSerializerOptions? options = null)
      {
         return serializer.Serialize(item, protocolVersion, writer, options);
      }
   }
}

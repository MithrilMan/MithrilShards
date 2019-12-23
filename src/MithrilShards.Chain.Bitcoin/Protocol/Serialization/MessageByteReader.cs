using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Text;
using MithrilShards.Chain.Bitcoin.Protocol.Serialization.Types;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization {
   public ref struct MessageByteReader {
      private const string NotEnoughBytesLeft = "Cannot read data, not enough bytes left.";
      private SequenceReader<byte> reader;

      public MessageByteReader(byte[] data) {
         var input = new ReadOnlySequence<byte>(data);
         this.reader = new SequenceReader<byte>(input);
      }

      public int ReadInt(bool isBigEndian = false) {
         if (isBigEndian) {
            return this.reader.TryReadBigEndian(out int value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return this.reader.TryReadLittleEndian(out int value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public uint ReadUInt(bool isBigEndian = false) {
         if (isBigEndian) {
            return this.reader.TryReadBigEndian(out int value) ? (uint)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return this.reader.TryReadLittleEndian(out int value) ? (uint)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public long ReadLong(bool isBigEndian = false) {
         if (isBigEndian) {
            return this.reader.TryReadBigEndian(out long value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return this.reader.TryReadLittleEndian(out long value) ? value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public ulong ReadULong(bool isBigEndian = false) {
         if (isBigEndian) {
            return this.reader.TryReadBigEndian(out long value) ? (ulong)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
         else {
            return this.reader.TryReadLittleEndian(out long value) ? (ulong)value : throw new MessageSerializationException(NotEnoughBytesLeft);
         }
      }

      public NetworkAddress ReadNetworkAddress() {
         NetworkAddress result = new NetworkAddress();
         var innerReader = new SequenceReader<byte>(this.reader.Sequence.Slice(this.reader.Position, result.Length));
         result.Deserialize(innerReader);
         this.reader.Advance(innerReader.Consumed);
         return result;
      }
   }
}

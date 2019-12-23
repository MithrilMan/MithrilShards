using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using NBitcoin.Protocol;

namespace MithrilShards.Network.Network {
   public class NetworkMessageDecoder {
      readonly ILogger<NetworkMessageDecoder> logger;

      public NetworkMessageDecoder(ILogger<NetworkMessageDecoder> logger) {
         this.logger = logger;
      }

      //public SequencePosition Decode(ReadOnlySequence<byte> input, out IEnumerable<ReadOnlyMemory<byte>> rawMessage) {
      //   IAsyncEnumerable
      //}

      public static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out Message message) {
         if (buffer.Length == 0) {
            message = null;
            return false;
         }

         using (MemoryStream ms = new MemoryStream(buffer.ToArray())) {
            try {
               message = Message.ReadNext(ms, NBitcoin.Network.TestNet, 70015, default, out _);
            }
            catch (Exception ex) {
               throw new InvalidNetworkMessageException("Invalid Network Message", ex);
               message = null;
               return false;
            }
            finally {
               buffer = buffer.Slice(ms.Position);
            }
            return true;
         }
      }

      bool HasMagicPacket(ReadOnlySequence<byte> buffer) {
         // buffer.TryGet(SequencePosition)
         return true;
      }
   }


   public class InvalidNetworkMessageException : Exception {
      public InvalidNetworkMessageException() { }
      public InvalidNetworkMessageException(string message) : base(message) { }
      public InvalidNetworkMessageException(string message, Exception inner) : base(message, inner) { }
      protected InvalidNetworkMessageException(
       System.Runtime.Serialization.SerializationInfo info,
       System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
   }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Network.Bedrock
{
   public class NetworkMessageWriter : INetworkMessageWriter
   {
      readonly ProtocolWriter writer;
      readonly IMessageWriter<INetworkMessage> messageWriter;

      public NetworkMessageWriter(IMessageWriter<INetworkMessage> messageWriter, ProtocolWriter writer)
      {
         this.messageWriter = messageWriter;
         this.writer = writer;
      }

      public ValueTask WriteAsync(INetworkMessage message, CancellationToken cancellationToken = default)
      {
         return this.writer.WriteAsync(this.messageWriter, message, cancellationToken);
      }

      public ValueTask WriteManyAsync(IEnumerable<INetworkMessage> messages, CancellationToken cancellationToken = default)
      {
         return this.writer.WriteManyAsync(this.messageWriter, messages, cancellationToken);
      }
   }
}

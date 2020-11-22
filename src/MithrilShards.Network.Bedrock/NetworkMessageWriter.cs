using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Network.Bedrock
{
   public class NetworkMessageWriter : INetworkMessageWriter
   {
      readonly ProtocolWriter _writer;
      readonly IMessageWriter<INetworkMessage> _messageWriter;

      public NetworkMessageWriter(IMessageWriter<INetworkMessage> messageWriter, ProtocolWriter writer)
      {
         _messageWriter = messageWriter;
         _writer = writer;
      }

      public ValueTask WriteAsync(INetworkMessage message, CancellationToken cancellationToken = default)
      {
         return _writer.WriteAsync(_messageWriter, message, cancellationToken);
      }

      public ValueTask WriteManyAsync(IEnumerable<INetworkMessage> messages, CancellationToken cancellationToken = default)
      {
         return _writer.WriteManyAsync(_messageWriter, messages, cancellationToken);
      }
   }
}

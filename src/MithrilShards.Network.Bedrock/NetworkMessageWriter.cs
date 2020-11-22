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
         this._messageWriter = messageWriter;
         this._writer = writer;
      }

      public ValueTask WriteAsync(INetworkMessage message, CancellationToken cancellationToken = default)
      {
         return this._writer.WriteAsync(this._messageWriter, message, cancellationToken);
      }

      public ValueTask WriteManyAsync(IEnumerable<INetworkMessage> messages, CancellationToken cancellationToken = default)
      {
         return this._writer.WriteManyAsync(this._messageWriter, messages, cancellationToken);
      }
   }
}

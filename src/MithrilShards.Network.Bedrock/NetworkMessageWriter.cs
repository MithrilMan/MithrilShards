using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using MithrilShards.Core.Network.Protocol;

namespace MithrilShards.Network.Bedrock {
   public class NetworkMessageWriter : INetworkMessageWriter {

      public NetworkMessageWriter(ProtocolWriter<INetworkMessage> writer) {
         this.Writer = writer;
      }

      public ProtocolWriter<INetworkMessage> Writer { get; }


      public ValueTask WriteAsync(INetworkMessage message, CancellationToken cancellationToken = default) {
         return this.Writer.WriteAsync(message, cancellationToken);
      }

      public ValueTask WriteManyAsync(IEnumerable<INetworkMessage> messages, CancellationToken cancellationToken = default) {
         return this.Writer.WriteManyAsync(messages, cancellationToken);
      }
   }
}

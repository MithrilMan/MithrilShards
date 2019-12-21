using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Network.Protocol;
using System.Threading.Tasks;

namespace MithrilShards.P2P.Bedrock {
   public class MithrilForgeConnectionHandler : ConnectionHandler {
      private readonly ILogger logger;
      readonly ILoggerFactory loggerFactory;
      private readonly IChainDefinition chainDefinition;

      public MithrilForgeConnectionHandler(ILogger<MithrilForgeConnectionHandler> logger, ILoggerFactory loggerFactory, IChainDefinition chainDefinition) {
         this.logger = logger;
         this.loggerFactory = loggerFactory;
         this.chainDefinition = chainDefinition;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection) {
         // Use a length prefixed protocol
         NetworkMessageProtocol protocol = new NetworkMessageProtocol(this.loggerFactory.CreateLogger<NetworkMessageProtocol>(), this.chainDefinition);
         ProtocolReader<Message> reader = Protocol.CreateReader(connection, protocol);
         ProtocolWriter<Message> writer = Protocol.CreateWriter(connection, protocol);

         while (true) {
            Message message = await reader.ReadAsync();

            this.logger.LogInformation("Received a message of {Length} bytes", message.Payload.Length);

            // REVIEW: We need a ReadResult<T> to indicate completion and cancellation
            if (message.Payload == null) {
               break;
            }
         }
      }
   }
}

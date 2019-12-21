using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MithrilShards.P2P.Bedrock {
   public class MithrilForgeConnectionHandler : ConnectionHandler {
      private readonly ILogger _logger;

      public MithrilForgeConnectionHandler(ILogger<MithrilForgeConnectionHandler> logger) {
         _logger = logger;
      }

      public override async Task OnConnectedAsync(ConnectionContext connection) {
         // Use a length prefixed protocol
         NetworkMessageProtocol protocol = new NetworkMessageProtocol();
         ProtocolReader<Message> reader = Protocol.CreateReader(connection, protocol);
         var writer = Protocol.CreateWriter(connection, protocol);

         while (true) {
            Message message = await reader.ReadAsync();

            _logger.LogInformation("Received a message of {Length} bytes", message.Payload.Length);

            // REVIEW: We need a ReadResult<T> to indicate completion and cancellation
            if (message.Payload == null) {
               break;
            }
         }
      }
   }
}

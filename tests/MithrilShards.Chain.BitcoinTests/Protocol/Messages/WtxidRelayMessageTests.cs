using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Protocol.Messages
{
   public class WtxidRelayMessageTests
   {
      [Fact]
      public void Command_Is_Correct()
      {
         // Arrange
         var message = new WtxidRelayMessage();

         // Act
         string command = ((INetworkMessage)message).Command;

         // Assert
         Assert.Equal(WtxidRelayMessage.COMMAND, command);
         Assert.Equal("wtxidrelay", command);
      }
   }
}

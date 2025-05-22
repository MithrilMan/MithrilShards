using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Protocol.Messages
{
   public class SendAddrv2MessageTests
   {
      [Fact]
      public void Command_Is_Correct()
      {
         // Arrange
         var message = new SendAddrv2Message();

         // Act
         string command = ((INetworkMessage)message).Command;

         // Assert
         Assert.Equal(SendAddrv2Message.COMMAND, command);
         Assert.Equal("sendaddrv2", command);
      }
   }
}

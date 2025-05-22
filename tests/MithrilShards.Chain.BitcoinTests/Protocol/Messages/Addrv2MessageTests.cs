using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Protocol.Messages
{
   public class Addrv2MessageTests
   {
      [Fact]
      public void Command_Is_Correct()
      {
         // Arrange
         var message = new Addrv2Message();

         // Act
         string command = ((INetworkMessage)message).Command;

         // Assert
         Assert.Equal(Addrv2Message.COMMAND, command);
         Assert.Equal("addrv2", command);
      }
   }
}

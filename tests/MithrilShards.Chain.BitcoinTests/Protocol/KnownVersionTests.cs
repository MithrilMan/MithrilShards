using MithrilShards.Chain.Bitcoin.Protocol;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Protocol
{
   public class KnownVersionTests
   {
      [Fact]
      public void CurrentVersion_Returns_Correct_Version()
      {
         // Arrange
         int expectedVersion = KnownVersion.V70016; // This was set in Task 1.1

         // Act
         int actualVersion = KnownVersion.CurrentVersion;

         // Assert
         Assert.Equal(expectedVersion, actualVersion);
      }
   }
}

using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol;
using Xunit;

namespace MithrilShards.Chain.BitcoinTests.Network
{
   public class NodeImplementationTests
   {
      [Fact]
      public void NodeImplementation_Properties_AreSetCorrectly()
      {
         // Arrange
         int expectedMinVersion = KnownVersion.V70012;
         int expectedCurrentVersion = KnownVersion.CurrentVersion; // Should be V70016

         // Act
         // This mimics how NodeImplementation is instantiated in ForgeBuilderExtensions after Task 1.1 changes
         var nodeImplementation = new NodeImplementation(expectedMinVersion, expectedCurrentVersion);

         // Assert
         Assert.Equal(expectedCurrentVersion, nodeImplementation.ImplementationVersion);
         Assert.Equal(expectedMinVersion, nodeImplementation.MinimumSupportedVersion);
      }
   }
}

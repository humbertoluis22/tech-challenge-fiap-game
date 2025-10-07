using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Tests
{
    public class LibraryItemTests
    {
        [Fact]
        public void Constructor_ShouldInitializeLibraryItem_WithDefaultValues()
        {
            // Arrange & Act
            var libraryItem = new LibraryItem();

            // Assert
            Assert.NotEqual(Guid.Empty, libraryItem.Id);
            Assert.Equal(Guid.Empty, libraryItem.UserLibraryId);
            Assert.Equal(Guid.Empty, libraryItem.GameId);
            Assert.True((DateTime.UtcNow - libraryItem.PurchasedAt) < TimeSpan.FromSeconds(5));
            Assert.Equal(0, libraryItem.PurchasePrice);
            Assert.Null(libraryItem.Game);
            Assert.Null(libraryItem.UserLibrary);
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var libraryItem = new LibraryItem();
            var userLibraryId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var purchasePrice = 79.90m;

            // Act
            libraryItem.UserLibraryId = userLibraryId;
            libraryItem.GameId = gameId;
            libraryItem.PurchasePrice = purchasePrice;

            // Assert
            Assert.Equal(userLibraryId, libraryItem.UserLibraryId);
            Assert.Equal(gameId, libraryItem.GameId);
            Assert.Equal(purchasePrice, libraryItem.PurchasePrice);
        }
    }
}
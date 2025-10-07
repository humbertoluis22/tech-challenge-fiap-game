using System;
using System.Linq;
using TechChallengeGame.Domain.Entities;
using Xunit;

namespace TechChallengeGame.Tests
{
    public class UserLibraryTests
    {
        #region Factory Method Tests (Create)

        [Fact]
        public void Create_ShouldCreateUserLibrary_WithCorrectUserIdAndEmptyItems()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var userLibrary = UserLibrary.Create(userId);

            // Assert
            Assert.NotNull(userLibrary);
            Assert.Equal(userId, userLibrary.UserId);
            Assert.NotEqual(Guid.Empty, userLibrary.Id);
            Assert.NotNull(userLibrary.Items);
            Assert.Empty(userLibrary.Items);
        }

        #endregion

        #region AddGame Tests

        [Fact]
        public void AddGame_ShouldAddItemToLibrary_WhenCalled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var purchasePrice = 49.99m;
            var userLibrary = UserLibrary.Create(userId);

            // Act
            userLibrary.AddGame(gameId, purchasePrice);

            // Assert
            Assert.Single(userLibrary.Items); // Verifica se a coleção tem exatamente 1 item

            var addedItem = userLibrary.Items.First();
            Assert.Equal(gameId, addedItem.GameId);
            Assert.Equal(purchasePrice, addedItem.PurchasePrice);
            Assert.Equal(userLibrary.Id, addedItem.UserLibraryId);
            // Verifica se a data é recente
            Assert.True((DateTime.UtcNow - addedItem.PurchasedAt) < TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void AddGame_ShouldAddMultipleItems_WhenCalledMultipleTimes()
        {
            // Arrange
            var userLibrary = UserLibrary.Create(Guid.NewGuid());
            var gameId1 = Guid.NewGuid();
            var gameId2 = Guid.NewGuid();

            // Act
            userLibrary.AddGame(gameId1, 10m);
            userLibrary.AddGame(gameId2, 20m);

            // Assert
            Assert.Equal(2, userLibrary.Items.Count);
            Assert.Contains(userLibrary.Items, item => item.GameId == gameId1);
            Assert.Contains(userLibrary.Items, item => item.GameId == gameId2);
        }

        #endregion

        #region RemoveGame Tests

        [Fact]
        public void RemoveGame_ShouldRemoveItemAndReturnIt_WhenGameExistsInLibrary()
        {
            // Arrange
            var userLibrary = UserLibrary.Create(Guid.NewGuid());
            var gameIdToRemove = Guid.NewGuid();

            userLibrary.AddGame(Guid.NewGuid(), 15m);
            userLibrary.AddGame(gameIdToRemove, 25m);
            userLibrary.AddGame(Guid.NewGuid(), 35m);

            var initialCount = userLibrary.Items.Count;

            // Act
            var removedItem = userLibrary.RemoveGame(gameIdToRemove);

            // Assert
            Assert.Equal(initialCount - 1, userLibrary.Items.Count);
            Assert.DoesNotContain(userLibrary.Items, item => item.GameId == gameIdToRemove);

            Assert.NotNull(removedItem);
            Assert.Equal(gameIdToRemove, removedItem.GameId);
        }

        [Fact]
        public void RemoveGame_ShouldReturnNull_WhenGameDoesNotExistInLibrary()
        {
            // Arrange
            var userLibrary = UserLibrary.Create(Guid.NewGuid());
            var nonExistentGameId = Guid.NewGuid();

            userLibrary.AddGame(Guid.NewGuid(), 15m);
            var initialCount = userLibrary.Items.Count;

            // Act
            var removedItem = userLibrary.RemoveGame(nonExistentGameId);

            // Assert
            Assert.Null(removedItem);
            Assert.Equal(initialCount, userLibrary.Items.Count);
        }

        [Fact]
        public void RemoveGame_ShouldReturnNull_WhenLibraryIsEmpty()
        {
            // Arrange
            var userLibrary = UserLibrary.Create(Guid.NewGuid());
            var gameId = Guid.NewGuid();

            // Act
            var removedItem = userLibrary.RemoveGame(gameId);

            // Assert
            Assert.Null(removedItem);
            Assert.Empty(userLibrary.Items);
        }

        #endregion
    }
}
using System;
using TechChallengeGame.Domain.Entities;
using Xunit;

namespace TechChallengeGame.Tests
{
    public class GameTests
    {
        [Fact]
        public void Constructor_ShouldInitializeGame_WithDefaultValues()
        {
            // Arrange & Act
            var game = new Game();

            // Assert
            Assert.NotEqual(Guid.Empty, game.Id);
            Assert.Empty(game.Name);
            Assert.Empty(game.Description);
            Assert.Empty(game.Genre);
            Assert.Equal(0, game.Price);
            Assert.False(game.IsActive);
            Assert.Equal(default(DateTime), game.ReleaseDate);
            Assert.Null(game.UpdatedAt);
            Assert.NotNull(game.LibraryItems);
            Assert.Empty(game.LibraryItems);
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var game = new Game();
            var releaseDate = new DateTime(2023, 10, 27);

            // Act
            game.Name = "Awesome Game";
            game.Description = "An awesome game description.";
            game.Genre = "Adventure";
            game.Price = 19.99m;
            game.IsActive = true;
            game.ReleaseDate = releaseDate;
            game.UpdatedAt = DateTime.UtcNow;

            // Assert
            Assert.Equal("Awesome Game", game.Name);
            Assert.Equal("An awesome game description.", game.Description);
            Assert.Equal("Adventure", game.Genre);
            Assert.Equal(19.99m, game.Price);
            Assert.True(game.IsActive);
            Assert.Equal(releaseDate, game.ReleaseDate);
            Assert.NotNull(game.UpdatedAt);
        }
    }
}
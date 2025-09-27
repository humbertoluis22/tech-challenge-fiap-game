using System;
using FluentAssertions;
using TechChallengeGame.Domain.Entities;
using Xunit;

namespace TechChallengeGame.Tests.Entities
{
    public class GameTests
    {
        [Fact]
        public void Constructor_ShouldInitializeGame_WithDefaultValues()
        {
            // Arrange & Act
            var game = new Game();

            // Assert
            game.Id.Should().NotBe(Guid.Empty);
            game.Name.Should().BeEmpty();
            game.Description.Should().BeEmpty();
            game.Genre.Should().BeEmpty();
            game.Price.Should().Be(0);
            game.IsActive.Should().BeFalse(); // O valor default de um bool é false
            game.ReleaseDate.Should().Be(default(DateTime));
            game.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            game.UpdatedAt.Should().BeNull();
            game.LibraryItems.Should().NotBeNull().And.BeEmpty();
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
            game.Name.Should().Be("Awesome Game");
            game.Description.Should().Be("An awesome game description.");
            game.Genre.Should().Be("Adventure");
            game.Price.Should().Be(19.99m);
            game.IsActive.Should().BeTrue();
            game.ReleaseDate.Should().Be(releaseDate);
            game.UpdatedAt.Should().NotBeNull();
        }
    }
}
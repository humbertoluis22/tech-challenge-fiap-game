using System;
using TechChallengeGame.Domain.Entities;
using Xunit;

namespace TechChallengeGame.Tests.Entities
{
    public class PromotionTests
    {
        [Fact]
        public void Constructor_ShouldInitializePromotion_WithDefaultValues()
        {
            // Arrange & Act
            var promotion = new Promotion();

            // Assert
            Assert.NotEqual(Guid.Empty, promotion.Id);
            Assert.Empty(promotion.Name);
            Assert.Equal(default(DateTime), promotion.StartDate);
            Assert.Equal(default(DateTime), promotion.EndDate);
            Assert.NotNull(promotion.GamesOnSale);
            Assert.Empty(promotion.GamesOnSale);
        }

        [Fact]
        public void Properties_ShouldBeSettable_And_GamesOnSaleShouldBeMutable()
        {
            // Arrange
            var promotion = new Promotion();
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(15);
            var gameOnSale = new PromotionGame { GameId = Guid.NewGuid(), DiscountPercentage = 50 };

            // Act
            promotion.Name = "Summer Sale";
            promotion.StartDate = startDate;
            promotion.EndDate = endDate;
            promotion.GamesOnSale.Add(gameOnSale);

            // Assert
            Assert.Equal("Summer Sale", promotion.Name);
            Assert.Equal(startDate, promotion.StartDate);
            Assert.Equal(endDate, promotion.EndDate);
            Assert.Single(promotion.GamesOnSale);
            Assert.Contains(gameOnSale, promotion.GamesOnSale);
        }
    }
}
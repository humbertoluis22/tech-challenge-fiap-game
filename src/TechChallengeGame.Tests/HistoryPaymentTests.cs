using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Entities.Enums;

namespace TechChallengeGame.Tests.Entities
{
    public class HistoryPaymentTests
    {
        [Fact]
        public void Constructor_ShouldInitializeHistoryPayment_WithDefaultValues()
        {
            // Arrange & Act
            var historyPayment = new HistoryPayment();

            // Assert
            Assert.NotEqual(Guid.Empty, historyPayment.Id);
            Assert.Null(historyPayment.PaymentTransactionId);
            Assert.Equal(StatusTransaction.Started, historyPayment.Status); // Default de Enum é o valor 0
            Assert.Equal(TransactionType.Purchase, historyPayment.Type); // Default de Enum é o valor 0
            Assert.True((DateTime.UtcNow - historyPayment.CreatedAt) < TimeSpan.FromSeconds(5));
            Assert.Null(historyPayment.UpdatedAt);
            Assert.NotNull(historyPayment.TransactionGames);
            Assert.Empty(historyPayment.TransactionGames);
        }
    }
}

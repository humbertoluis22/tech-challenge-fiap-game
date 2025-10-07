using TechChallengeGame.Domain.Entities.Enums;

namespace TechChallengeGame.Domain.Entities
{
    public class HistoryPayment:Entity
    {
        public Guid? PaymentTransactionId { get; set; }
        public StatusTransaction Status { get; set; }
        public TransactionType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public List<TransactionGame> TransactionGames { get; set; } = [];
    }
}

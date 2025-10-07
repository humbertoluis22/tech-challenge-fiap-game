namespace TechChallengeGame.Domain.Entities
{
    public class TransactionGame
    {
        public Guid HistoryPaymentId { get; set; }
        public Guid GameId { get; set; }
        public Guid? PromotionId { get; set; }

        // EF Core relationships
        public HistoryPayment HistoryPayment { get; set; }
        public Game Game { get; set; }
        public Promotion Promotion { get; set; }
    }
}

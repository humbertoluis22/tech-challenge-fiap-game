namespace TechChallengeGame.Shared.Models.Dtos.Responses
{
    public class TransactionGameResponse
    {
        public Guid HistoryPaymentId { get; set; }
        public Guid GameId { get; set; }
        public Guid? PromotionId { get; set; }
    }
}

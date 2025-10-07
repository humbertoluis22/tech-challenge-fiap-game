namespace TechChallengeGame.Shared.Models.Dtos.Events
{
    public class GameDetail
    {
        public Guid GameId { get; set; }
        public decimal Price { get; set; }
        public Guid? PromotionId { get; set; }
        public decimal? Discount { get; set; }
        public Guid HistoryPaymentId { get; set; }
    }
}

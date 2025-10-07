namespace TechChallengeGame.Shared.Models.Dtos.Commands
{
    public class GamePromotion
    {
        public Guid HistoryPaymentId { get; set; }
        public Guid GameId { get; set; }
        public decimal Price { get; set; }
        public Guid? PromotionId { get; set; }
        public decimal? Discount { get; set; }
    }
}

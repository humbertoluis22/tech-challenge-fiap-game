using System.ComponentModel.DataAnnotations;

namespace TechChallengeGame.Shared.Models.Dtos.Requests
{
    public class GamePromotionRequest
    {
        [Required]
        public Guid GameId { get; set; }
        public Guid? PromotionGameId { get; set; }
    }
}

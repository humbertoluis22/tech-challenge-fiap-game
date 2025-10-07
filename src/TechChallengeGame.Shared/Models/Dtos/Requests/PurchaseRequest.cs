using System.ComponentModel.DataAnnotations;

namespace TechChallengeGame.Shared.Models.Dtos.Requests
{
    public class PurchaseRequest
    {
        
        [Required]
        public List<GamePromotionRequest> Games { get; set; }
    }
}

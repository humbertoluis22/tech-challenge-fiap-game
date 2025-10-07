using System.ComponentModel.DataAnnotations;

namespace TechChallengeGame.Shared.Models.Dtos.Commands
{
    public record CreatePurchaseCommand(
        [Required] Guid UserId,
        [Required] List<GamePromotion> Games
    );
}

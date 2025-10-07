using System.ComponentModel.DataAnnotations;

namespace TechChallengeGame.Shared.Models.Dtos.Commands
{
    public record RefundRequestedEvent(
        [Required] Guid UserId,
        [Required] Guid PaymentTransactionId

    );
}

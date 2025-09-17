namespace TechChallengeGame.Shared.Models.Dtos.Events;

public class FundsDebitedEvent
{
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal Amount { get; set; }
    public Guid CorrelationId { get; set; } // Essencial para rastreabilidade
}
namespace TechChallengeGame.Shared.Models.Dtos.Events;

public class GameAddedToLibraryEvent
{
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public DateTime AddedAt { get; set; }
    public Guid CorrelationId { get; set; } // Propagar o ID de correlação é crucial
}

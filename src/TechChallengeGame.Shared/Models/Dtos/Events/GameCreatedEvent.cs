namespace TechChallengeGame.Shared.Models.Dtos.Events
{
    public class GameCreatedEvent
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

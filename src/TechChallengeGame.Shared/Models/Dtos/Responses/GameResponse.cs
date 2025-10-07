using System.Text.Json.Serialization;

namespace TechChallengeGame.Shared.Models.Dtos.Responses
{
    public class GameResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? UpdatedAt { get; set; }
    }
}


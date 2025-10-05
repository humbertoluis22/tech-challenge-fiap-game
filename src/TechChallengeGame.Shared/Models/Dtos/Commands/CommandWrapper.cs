using System.Text.Json.Serialization;

namespace TechChallengeGame.Shared.Models.Dtos.Commands
{
    public class CommandWrapper<T>
    {
        [JsonPropertyName("CommandType")]
        public string CommandType { get; set; }

        [JsonPropertyName("Payload")]
        public T Payload { get; set; }
    }
}
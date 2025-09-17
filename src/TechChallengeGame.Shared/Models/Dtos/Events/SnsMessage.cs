using System.Text.Json.Serialization;

namespace TechChallengeGame.Application.Models.Dtos.Events;

public class SnsMessage
{
    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("TopicArn")]
    public string TopicArn { get; set; } = string.Empty;
}
using System.Text.Json.Serialization;

namespace TechChallengeGame.Shared.Models.Dtos.Events
{
    public class SnsMessage
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("MessageId")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("TopicArn")]
        public string TopicArn { get; set; } = string.Empty;

        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("Timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("SignatureVersion")]
        public string SignatureVersion { get; set; } = string.Empty;

        [JsonPropertyName("Signature")]
        public string Signature { get; set; } = string.Empty;

        [JsonPropertyName("SigningCertURL")]
        public string SigningCertURL { get; set; } = string.Empty;

        [JsonPropertyName("UnsubscribeURL")]
        public string UnsubscribeURL { get; set; } = string.Empty;

        // Este campo também pode estar presente, então é bom incluí-lo.
        [JsonPropertyName("MessageAttributes")]
        public object? MessageAttributes { get; set; }
    }
}
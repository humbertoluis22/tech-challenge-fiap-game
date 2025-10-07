namespace TechChallengeGame.Domain.Interfaces
{
    public interface IQueuePublisher
    {
        Task PublishAsync<T>(string queueUrl, T message, CancellationToken ct = default);
    }
}

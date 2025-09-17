using System.Threading.Tasks;

namespace TechChallengeGame.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default);
}
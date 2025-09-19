using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface IQueuePublisher
    {
        Task PublishAsync<T>(string queueUrl, T message, CancellationToken ct = default);
    }
}

using Amazon.SQS;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TechChallengeGame.Domain.Interfaces;

namespace TechChallengeGame.Domain.Services
{
    public class SqsQueuePublisher : IQueuePublisher
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger<SqsQueuePublisher> _logger;

        public SqsQueuePublisher(IAmazonSQS sqsClient, ILogger<SqsQueuePublisher> logger)
        {
            _sqsClient = sqsClient;
            _logger = logger;
        }

        public async Task PublishAsync<T>(string queueUrl, T message, CancellationToken ct = default)
        {
            var messageJson = JsonSerializer.Serialize(message);

            var sendMessageRequest = new Amazon.SQS.Model.SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = messageJson
            };

            try
            {
                var response = await _sqsClient.SendMessageAsync(sendMessageRequest, ct);
                _logger.LogInformation("Mensagem para {QueueUrl} publicada com sucesso. MessageId: {MessageId}", queueUrl, response.MessageId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem na fila SQS {QueueUrl}.", queueUrl);
                throw;
            }
        }
    }
}

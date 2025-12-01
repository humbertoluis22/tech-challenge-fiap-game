using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;
using System.Text.Json;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Infrastructure.Resilience; 

namespace TechChallengeGame.Infrastructure.Services; 

public class SqsQueuePublisher : IQueuePublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<SqsQueuePublisher> _logger;
    private readonly AsyncPolicyWrap _resiliencePolicy;

    public SqsQueuePublisher(IAmazonSQS sqsClient, ILogger<SqsQueuePublisher> logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;

        // As políticas de resiliência também estão na camada de Infraestrutura, o que é correto
        var retry = ResiliencePolicies.CreateSqsRetryPolicy(_logger);
        var circuitBreaker = ResiliencePolicies.CreateSqsCircuitBreakerPolicy(_logger);
        _resiliencePolicy = Policy.WrapAsync(retry, circuitBreaker);
    }

    public async Task PublishAsync<T>(string queueUrl, T message, CancellationToken ct = default)
    {
        var messageJson = JsonSerializer.Serialize(message);
        var request = new SendMessageRequest { QueueUrl = queueUrl, MessageBody = messageJson };

        try
        {
            await _resiliencePolicy.ExecuteAsync(async (token) =>
            {
                var response = await _sqsClient.SendMessageAsync(request, token);
                _logger.LogInformation("Mensagem enviada com sucesso para SQS. ID: {MessageId}", response.MessageId);
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha crítica no envio para SQS (Esgotou tentativas ou Circuito Aberto).");
            throw;
        }
    }
}


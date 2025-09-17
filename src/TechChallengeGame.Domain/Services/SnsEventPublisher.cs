using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TechChallengeGame.Domain.Interfaces;

namespace TechChallengeGame.Application.Services;

// Classe para carregar as configurações do appsettings.json
public class SnsPublisherOptions
{
    public const string SectionName = "SnsPublisher";
    public string TopicArn { get; set; } = string.Empty;
}

public class SnsEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly SnsPublisherOptions _options;
    private readonly ILogger<SnsEventPublisher> _logger;

    public SnsEventPublisher(
        IAmazonSimpleNotificationService snsClient,
        IOptions<SnsPublisherOptions> options,
        ILogger<SnsEventPublisher> logger)
    {
        _snsClient = snsClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken ct = default)
    {
        var messageJson = JsonSerializer.Serialize(message);

        var publishRequest = new PublishRequest
        {
            TopicArn = _options.TopicArn,
            Message = messageJson,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {
                    "EventType", new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = typeof(T).Name // Ex: "GameAddedToLibraryEvent"
                    }
                }
            }
        };

        try
        {
            var response = await _snsClient.PublishAsync(publishRequest, ct);
            _logger.LogInformation("Evento {EventType} publicado com sucesso no SNS. MessageId: {MessageId}", typeof(T).Name, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar evento {EventType} no SNS.", typeof(T).Name);
            // Lançar a exceção permite que o chamador decida como lidar com a falha
            // (ex: não deletar a mensagem SQS para tentar novamente)
            throw;
        }
    }
}
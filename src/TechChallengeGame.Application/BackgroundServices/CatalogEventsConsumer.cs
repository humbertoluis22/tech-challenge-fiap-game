using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Shared.Models.Dtos.Events;

namespace TechChallengeGame.Application.BackgroundServices;

public class SqsConsumerOptions
{
    public const string SectionName = "SqsConsumer";
    public string QueueUrl { get; set; } = string.Empty;
}

public class CatalogEventsConsumer : BackgroundService
{
    private readonly ILogger<CatalogEventsConsumer> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly SqsConsumerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public CatalogEventsConsumer(
        ILogger<CatalogEventsConsumer> logger,
        IAmazonSQS sqsClient,
        IOptions<SqsConsumerOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _options = options.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Iniciando SQS Consumer para a fila: {QueueUrl}", _options.QueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var receiveRequest = new ReceiveMessageRequest
            {
                QueueUrl = _options.QueueUrl,
                MaxNumberOfMessages = 5,
                WaitTimeSeconds = 10
            };

            try
            {
                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (response.Messages.Any())
                {
                    _logger.LogInformation("{Count} mensagens recebidas.", response.Messages.Count);
                    var processingTasks = response.Messages.Select(message => ProcessMessageAsync(message, stoppingToken));
                    await Task.WhenAll(processingTasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no consumer SQS. Aguardando para tentar novamente.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken stoppingToken)
    {
        try
        {
            var snsMessage = JsonSerializer.Deserialize<SnsMessage>(message.Body);
            if (snsMessage?.Type != "Notification")
            {
                _logger.LogWarning("Mensagem recebida não é do tipo 'Notification'. Ignorando e removendo da fila.");
                await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
                return;
            }

            var fundsDebitedEvent = JsonSerializer.Deserialize<FundsDebitedEvent>(snsMessage.Message);
            if (fundsDebitedEvent is null)
            {
                _logger.LogError("Não foi possível desserializar o evento 'FundsDebited'. Body: {Body}", snsMessage.Message);
                await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
                return;
            }

            _logger.LogInformation("Evento 'FundsDebited' recebido. CorrelationId: {CorrelationId}", fundsDebitedEvent.CorrelationId);

            using (var scope = _scopeFactory.CreateScope())
            {
                var userLibraryService = scope.ServiceProvider.GetRequiredService<IUserLibraryService>();
                var result = await userLibraryService.AddGameToLibraryFromEventAsync(fundsDebitedEvent.UserId, fundsDebitedEvent.GameId, stoppingToken);

                if (result is not true)
                {
                    _logger.LogWarning("Falha na lógica de negócio ao processar evento para o usuário {UserId}. A mensagem será processada novamente.", fundsDebitedEvent.UserId);
                    return;
                }

                // Lógica de publicação movida para dentro do escopo
                var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
                var gameAddedEvent = new GameAddedToLibraryEvent
                {
                    UserId = fundsDebitedEvent.UserId,
                    GameId = fundsDebitedEvent.GameId,
                    AddedAt = DateTime.UtcNow,
                    CorrelationId = fundsDebitedEvent.CorrelationId // Propaga o ID de correlação
                };

                await eventPublisher.PublishAsync(gameAddedEvent, stoppingToken);

                _logger.LogInformation("Jogo {GameId} adicionado à biblioteca e evento 'GameAddedToLibrary' publicado.", fundsDebitedEvent.GameId);
            }

            // A mensagem só é deletada se tudo acima (adição no BD e publicação no SNS) ocorrer sem exceções
            await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Erro de desserialização. A mensagem será removida para evitar envenenamento da fila. Conteúdo: {Body}", message.Body);
            await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar a mensagem {MessageId}. Ela será reprocessada.", message.MessageId);
            // Em caso de erro (ex: falha ao publicar no SNS), a mensagem não é deletada e voltará para a fila.
        }
    }

    private async Task DeleteMessageFromQueue(string receiptHandle, CancellationToken ct)
    {
        await _sqsClient.DeleteMessageAsync(_options.QueueUrl, receiptHandle, ct);
        _logger.LogInformation("Mensagem processada com sucesso e removida da fila SQS.");
    }
}
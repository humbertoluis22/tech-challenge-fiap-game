using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TechChallengeGame.Application.Models.Dtos.Events;
using TechChallengeGame.Domain.Interfaces;

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

            // Criamos um escopo para resolver dependências Scoped (como DbContext e UnitOfWork)
            using (var scope = _scopeFactory.CreateScope())
            {
                var userLibraryService = scope.ServiceProvider.GetRequiredService<IUserLibraryService>();
                var result = await userLibraryService.AddGameToLibraryFromEventAsync(fundsDebitedEvent.UserId, fundsDebitedEvent.GameId, stoppingToken);

                if (result is not true)
                {
                    _logger.LogWarning("Falha na lógica de negócio ao processar evento para o usuário {UserId}. A mensagem será processada novamente.", fundsDebitedEvent.UserId);
                    // Não deletamos a mensagem para que ela seja reprocessada.
                    // ATENÇÃO: É crucial ter uma Dead-Letter Queue (DLQ) configurada na AWS para evitar loops infinitos.
                    return;
                }
            }

            // TODO: Publicar o próximo evento da Saga ('GameAddedToLibrary') no tópico SNS.
            _logger.LogInformation("Jogo {GameId} adicionado à biblioteca do usuário {UserId} com sucesso.", fundsDebitedEvent.GameId, fundsDebitedEvent.UserId);

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
        }
    }

    private async Task DeleteMessageFromQueue(string receiptHandle, CancellationToken ct)
    {
        await _sqsClient.DeleteMessageAsync(_options.QueueUrl, receiptHandle, ct);
        _logger.LogInformation("Mensagem removida da fila SQS.");
    }
}
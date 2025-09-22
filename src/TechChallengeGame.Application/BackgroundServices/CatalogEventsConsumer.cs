using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TechChallengeGame.Domain.Entities.Enums;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Shared.Models.Dtos.Events;

namespace TechChallengeGame.Application.BackgroundServices
{
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
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var snsMessage = JsonSerializer.Deserialize<SnsMessage>(message.Body, options);

                if (snsMessage?.Type != "Notification")
                {
                    _logger.LogWarning("Mensagem recebida não é do tipo 'Notification'. Ignorando.");
                    await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
                    return;
                }

                var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(snsMessage.Message, options);
                if (paymentEvent is null || !paymentEvent.Games.Any())
                {
                    _logger.LogError("Não foi possível desserializar o evento 'PaymentProcessed' ou a lista de jogos está vazia. Body: {Body}", snsMessage.Message);
                    await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
                    return;
                }

                _logger.LogInformation("Evento 'PaymentProcessed' recebido para o usuário {UserId}. Processando {GameCount} jogos.", paymentEvent.UserId, paymentEvent.Games.Count);

                // --- INÍCIO DA SOLUÇÃO ---

                // 1. Processa a adição de cada jogo em um escopo e transação isolados.
                bool allGamesAddedSuccessfully = true;
                foreach (var game in paymentEvent.Games)
                {
                    // Cria um escopo de injeção de dependência totalmente novo para esta operação.
                    using (var gameScope = _scopeFactory.CreateScope())
                    {
                        var userLibraryService = gameScope.ServiceProvider.GetRequiredService<IUserLibraryService>();
                        // A chamada abaixo agora funciona, pois o serviço criará sua própria transação em um escopo limpo.
                        var result = await userLibraryService.AddGameToLibraryFromEventAsync(paymentEvent.UserId, game.GameId, stoppingToken);

                        if (result is not true)
                        {
                            _logger.LogWarning("Falha na lógica de negócio ao adicionar o jogo {GameId} para o usuário {UserId}. Este jogo não foi adicionado.", game.GameId, paymentEvent.UserId);
                            allGamesAddedSuccessfully = false;
                            // Não damos 'break' para tentar adicionar os outros jogos da compra.
                        }
                    }
                }

                if (!allGamesAddedSuccessfully)
                {
                    _logger.LogError("Nem todos os jogos da transação {PaymentTransactionId} puderam ser adicionados. A atualização do HistoryPayment será abortada, mas os jogos bem-sucedidos permanecerão na biblioteca.", paymentEvent.PaymentTransactionId);
                    // A mensagem é deletada para não tentar reprocessar uma compra parcial.
                    await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
                    return;
                }

                // 2. Atualiza o HistoryPayment em seu próprio escopo e transação isolados.
                using (var finalScope = _scopeFactory.CreateScope())
                {
                    var historyPaymentRepo = finalScope.ServiceProvider.GetRequiredService<IHistoryPaymentRepository>();
                    var unitOfWork = finalScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    // Inicia a transação APENAS para esta operação de atualização.
                    await using var transaction = await unitOfWork.BeginTransactionAsync(stoppingToken);

                    try
                    {
                        var historyPaymentId = paymentEvent.Games.First().HistoryPaymentId;
                        // Usamos FirstOrDefault com trackChanges para otimizar o Update.
                        var historyPayment = await historyPaymentRepo.FirstOrDefaultAsync(p => p.Id == historyPaymentId, trackChanges: true);

                        if (historyPayment is null)
                        {
                            _logger.LogError("Não foi possível encontrar o HistoryPayment com ID {HistoryPaymentId} para atualizar.", historyPaymentId);
                            await unitOfWork.RollbackAsync(stoppingToken); // Cancela a transação local.
                        }
                        else
                        {
                            historyPayment.PaymentTransactionId = paymentEvent.PaymentTransactionId;
                            historyPayment.Status = StatusTransaction.Finished;
                            historyPayment.UpdatedAt = DateTime.UtcNow;

                            await unitOfWork.CommitAsync(stoppingToken);
                            _logger.LogInformation("HistoryPayment {HistoryPaymentId} atualizado com sucesso.", historyPaymentId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falha ao atualizar o HistoryPayment. Fazendo rollback da atualização.");
                        await unitOfWork.RollbackAsync(stoppingToken);
                        throw; // Lança a exceção para a mensagem ser reprocessada, pois os jogos foram adicionados mas o histórico não foi atualizado.
                    }
                }

                // --- FIM DA SOLUÇÃO ---

                await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Erro de desserialização. A mensagem será removida. Conteúdo: {Body}", message.Body);
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
            _logger.LogInformation("Mensagem processada com sucesso e removida da fila SQS.");
        }
    }
}
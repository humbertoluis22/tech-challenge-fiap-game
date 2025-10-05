using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes; 
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
                string actualMessageContent = message.Body; // 1. Assume que é uma mensagem direta por padrão.

                // 2. Tenta verificar se a mensagem é um "envelope" do SNS.
                try
                {
                    var snsMessage = JsonSerializer.Deserialize<SnsMessage>(message.Body, options);
                    if (snsMessage?.Type == "Notification" && !string.IsNullOrEmpty(snsMessage.Message))
                    {
                        // 3. Se for do SNS, extrai a mensagem interna.
                        actualMessageContent = snsMessage.Message;
                        _logger.LogInformation("Mensagem no formato SNS desembrulhada com sucesso.");
                    }
                }
                catch (JsonException)
                {
                    // Se falhar a desserialização, não é um problema. Apenas significa que não é um envelope SNS.
                    _logger.LogInformation("Mensagem recebida no formato SQS direto.");
                }

                // 4. A partir daqui, usa 'actualMessageContent' que contém o JSON do evento real.
                var commandTypeNode = JsonNode.Parse(actualMessageContent)?["CommandType"];
                var commandType = commandTypeNode?.GetValue<string>();

                _logger.LogInformation("Evento do tipo '{CommandType}' recebido para processamento.", commandType);

                bool success = false;
                switch (commandType)
                {
                    case "create-purchase":
                        // O evento que o serviço de pagamento envia de volta para o catálogo após processar a compra
                        var purchaseEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(actualMessageContent, options);
                        if (purchaseEvent != null)
                        {
                            success = await HandlePurchaseAsync(purchaseEvent, stoppingToken);
                        }
                        break;

                    case "create-refund":
                        // O evento que o serviço de pagamento envia de volta após processar o reembolso
                        var refundEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(actualMessageContent, options); // Assumindo que o evento de resposta tem a mesma estrutura
                        if (refundEvent != null)
                        {
                            success = await HandleRefundAsync(refundEvent, stoppingToken);
                        }
                        break;

                    default:
                        _logger.LogWarning("CommandType desconhecido ou ausente na mensagem: {CommandType}", commandType);
                        success = true; // Remove a mensagem da fila se não soubermos o que fazer com ela
                        break;
                }

                if (success)
                {
                    await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Erro de desserialização JSON no corpo da mensagem. A mensagem será removida. Conteúdo: {Body}", message.Body);
                await DeleteMessageFromQueue(message.ReceiptHandle, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar a mensagem {MessageId}. Ela não será removida e será reprocessada.", message.MessageId);
            }
        }

        private async Task<bool> HandlePurchaseAsync(PaymentProcessedEvent paymentEvent, CancellationToken stoppingToken)
        {
            // A lógica de compra que você já tinha
            bool allGamesAddedSuccessfully = true;
            foreach (var game in paymentEvent.Games)
            {
                using var gameScope = _scopeFactory.CreateScope();
                var userLibraryService = gameScope.ServiceProvider.GetRequiredService<IUserLibraryService>();
                var result = await userLibraryService.AddGameToLibraryFromEventAsync(paymentEvent.UserId, game.GameId, stoppingToken);

                if (result is not true)
                {
                    _logger.LogWarning("Falha na lógica de negócio ao adicionar o jogo {GameId} para o usuário {UserId}.", game.GameId, paymentEvent.UserId);
                    allGamesAddedSuccessfully = false;
                }
            }

            if (!allGamesAddedSuccessfully) return false; // Não deleta a msg se algum jogo falhou

            // Atualiza o HistoryPayment
            using var finalScope = _scopeFactory.CreateScope();
            var historyPaymentRepo = finalScope.ServiceProvider.GetRequiredService<IHistoryPaymentRepository>();
            var unitOfWork = finalScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await using var transaction = await unitOfWork.BeginTransactionAsync(stoppingToken);
            try
            {
                var historyPaymentId = paymentEvent.Games.First().HistoryPaymentId;
                var historyPayment = await historyPaymentRepo.FirstOrDefaultAsync(p => p.Id == historyPaymentId, trackChanges: true);

                if (historyPayment is not null)
                {
                    historyPayment.PaymentTransactionId = paymentEvent.PaymentTransactionId;
                    historyPayment.Status = StatusTransaction.Finished;
                    historyPayment.UpdatedAt = DateTime.UtcNow;
                    await unitOfWork.CommitAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao atualizar o HistoryPayment. Fazendo rollback.");
                await unitOfWork.RollbackAsync(stoppingToken);
                return false; // Retorna false para tentar novamente
            }

            return true;
        }

        private async Task<bool> HandleRefundAsync(PaymentProcessedEvent refundEvent, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando processo de reembolso para o usuário {UserId} referente à transação {PaymentTransactionId}.", refundEvent.UserId, refundEvent.PaymentTransactionId);

            using var scope = _scopeFactory.CreateScope();
            var historyPaymentRepo = scope.ServiceProvider.GetRequiredService<IHistoryPaymentRepository>();
            // Injetamos o repositório da biblioteca do usuário para fazer a query nova
            var userLibraryRepo = scope.ServiceProvider.GetRequiredService<IUserLibraryRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // A transação agora controla todo o processo, incluindo a deleção do jogo
            await using var transaction = await unitOfWork.BeginTransactionAsync(stoppingToken);

            try
            {
                var historyPayment = await historyPaymentRepo.FirstOrDefaultAsync(
                    p => p.PaymentTransactionId == refundEvent.PaymentTransactionId,
                    trackChanges: true,
                    includes: p => p.TransactionGames);

                if (historyPayment is null)
                {
                    _logger.LogError("Não foi possível encontrar a transação original com PaymentTransactionId {PaymentTransactionId}.", refundEvent.PaymentTransactionId);
                    await unitOfWork.RollbackAsync(stoppingToken);
                    return true; // Mensagem inválida, remove da fila
                }

                if (historyPayment.TransactionGames != null && historyPayment.TransactionGames.Any())
                {
                    _logger.LogInformation("Removendo {GameCount} jogos da biblioteca do usuário.", historyPayment.TransactionGames.Count);

                    // --- INÍCIO DA NOVA LÓGICA DE DELEÇÃO ---

                    // 1. Pega o ID da biblioteca do usuário de acordo com o UserId
                    var userLibrary = await userLibraryRepo.FirstOrDefaultAsync(
                        lib => lib.UserId == refundEvent.UserId,
                        trackChanges: false, // Não precisa rastrear a biblioteca principal
                        includes: lib => lib.Items); // Inclui os itens para encontrar o jogo a ser removido

                    if (userLibrary is null)
                    {
                        _logger.LogWarning("Biblioteca para o usuário {UserId} não encontrada. Não é possível remover os jogos.", refundEvent.UserId);
                        await unitOfWork.RollbackAsync(stoppingToken);
                        return false; // Falha, reprocessa
                    }

                    // 2. Para cada jogo da transação, encontra e remove o item correspondente da biblioteca
                    foreach (var gameTransaction in historyPayment.TransactionGames)
                    {
                        // 3. Vai na lista de LibraryItem e encontra o item com o GameId correspondente
                        var itemToRemove = userLibrary.Items.FirstOrDefault(item => item.GameId == gameTransaction.GameId);

                        if (itemToRemove != null)
                        {
                            // 4. Remove o jogo
                            userLibraryRepo.RemoveLibraryItem(itemToRemove);
                            _logger.LogInformation("Jogo {GameId} marcado para remoção da biblioteca {UserLibraryId}.", gameTransaction.GameId, userLibrary.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Jogo {GameId} não encontrado na biblioteca do usuário {UserId} para remoção.", gameTransaction.GameId, refundEvent.UserId);
                        }
                    }
                    // --- FIM DA NOVA LÓGICA DE DELEÇÃO ---
                }

                // Atualiza o HistoryPayment para refletir o reembolso
                historyPayment.Type = TransactionType.Refund;
                historyPayment.Status = StatusTransaction.Finished;
                historyPayment.UpdatedAt = DateTime.UtcNow;

                // Salva TODAS as alterações (deleção dos LibraryItems e atualização do HistoryPayment)
                await unitOfWork.CommitAsync(stoppingToken);
                _logger.LogInformation("HistoryPayment {HistoryPaymentId} atualizado para 'Refund' e jogos removidos com sucesso.", historyPayment.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha crítica ao processar o reembolso para a transação {PaymentTransactionId}. Fazendo rollback.", refundEvent.PaymentTransactionId);
                await unitOfWork.RollbackAsync(stoppingToken);
                return false; // Tenta novamente
            }
        }

        private async Task DeleteMessageFromQueue(string receiptHandle, CancellationToken ct)
        {
            await _sqsClient.DeleteMessageAsync(_options.QueueUrl, receiptHandle, ct);
            _logger.LogInformation("Mensagem processada e removida da fila SQS.");
        }
    }
}
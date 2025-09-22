using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Entities.Enums;
using TechChallengeGame.Domain.Interfaces;
using TechChallengeGame.Shared.Models.Dtos.Commands;
using TechChallengeGame.Shared.Models.Dtos.Requests;

namespace TechChallengeGame.Domain.Services
{
    public class TransactionService : BaseService, ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHistoryPaymentRepository _historyPaymentRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IPromotionRepository _promotionRepository;
        private readonly IQueuePublisher _queuePublisher;
        private readonly IConfiguration _configuration;
        private readonly IUserLibraryRepository _userLibraryRepository;
        private readonly IUserLibraryService _userLibraryService;

        public TransactionService(
            INotifier notifier,
            IUnitOfWork unitOfWork,
            IHistoryPaymentRepository historyPaymentRepository,
            IGameRepository gameRepository,
            IPromotionRepository promotionRepository,
            IQueuePublisher queuePublisher,
            IUserLibraryRepository userLibraryRepository,
            IUserLibraryService userLibraryService,
            IConfiguration configuration) : base(notifier)

        {
            _unitOfWork = unitOfWork;
            _historyPaymentRepository = historyPaymentRepository;
            _gameRepository = gameRepository;
            _promotionRepository = promotionRepository;
            _queuePublisher = queuePublisher;
            _configuration = configuration;
            _userLibraryRepository = userLibraryRepository;
            _userLibraryService = userLibraryService;
        }

        public async Task<HistoryPayment> CreatePurchaseAsync(PurchaseRequest request, CancellationToken ct = default)
        {
            var commandGames = new List<GamePromotion>();

            // 1. Validar cada item da compra
            foreach (var item in request.Games)
            {
                var game = await _gameRepository.GetByIdAsync(item.GameId, ct);
                if (game == null)
                {
                    Notify($"Jogo com ID '{item.GameId}' não encontrado.");
                    return null;
                }

                decimal? discount = null;
                Guid? promotionId = null;

                if (item.PromotionGameId.HasValue)
                {
                    var promotionGame = await _promotionRepository.FindByPromotionAndGameAsync(item.PromotionGameId.Value, item.GameId);

                    if (promotionGame == null)
                    {
                        Notify($"Promoção do jogo com ID '{item.PromotionGameId}' não encontrada.");
                        return null;
                    }

                    var promotion = await _promotionRepository.GetByIdAsync(promotionGame.PromotionId, ct);

                    if (promotion.StartDate > DateTime.UtcNow || promotion.EndDate < DateTime.UtcNow)
                    {
                        Notify($"A promoção '{promotion.Name}' não está ativa.");
                        return null;
                    }

                    if (promotionGame.GameId != item.GameId)
                    {
                        Notify($"O jogo '{game.Name}' não pertence à promoção informada.");
                        return null;
                    }

                    discount = promotionGame.DiscountPercentage;
                    promotionId = promotionGame.Id;
                }

                commandGames.Add(new GamePromotion
                {
                    GameId = game.Id,
                    Price = game.Price,
                    PromotionId = promotionId,
                    Discount = discount
                });
            }

            var historyPayment = new HistoryPayment
            {
                Status = StatusTransaction.Started,
                Type = TransactionType.Purchase,
                TransactionGames = request.Games.Select(g => new TransactionGame
                {
                    GameId = g.GameId,
                    PromotionId = g.PromotionGameId
                }).ToList()
            };

            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                await _historyPaymentRepository.AddAsync(historyPayment, ct);
                var success = await _unitOfWork.CommitAsync(ct);

                if (!success)
                {
                    Notify("Não foi possível salvar a transação.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync(ct);
                Notify($"Erro ao salvar a transação: {ex.Message}");
                return null;
            }

            // 3. Preparar e enviar o comando para a fila SQS
            // descomentar quando for implementar
            foreach (var game in commandGames)
            {
                game.HistoryPaymentId = historyPayment.Id;
            }

            var purchaseCommand = new CreatePurchaseCommand(request.UserId, commandGames);
            var queueUrl = _configuration["SqsPublisher:QueueUrl"];

            await _queuePublisher.PublishAsync(queueUrl, purchaseCommand, ct);

            return historyPayment;
        }


        public async Task<bool> CreateRefundAsync(RefundRequest request, CancellationToken ct = default)
        {
            
            var originalTransaction = await _historyPaymentRepository.GetByIdAsync(request.PaymentTransactionId, ct);

            if (originalTransaction is null)
            {
                Notify("Transação de pagamento não encontrada.");
                return false;
            }

            

            // 2. Publicar as informações na fila SQS
            var refundEvent = new RefundRequestedEvent(
                UserId: request.UserId,
                PaymentTransactionId: request.PaymentTransactionId
            );

            try
            {
                var queueUrl = _configuration["SqsPublisher:QueueUrl"];
                if (string.IsNullOrEmpty(queueUrl))
                {
                    Notify("A URL da fila SQS para publicação não está configurada.");
                    return false;
                }

                await _queuePublisher.PublishAsync(queueUrl, refundEvent, ct);
            }
            catch (Exception ex)
            {
                Notify($"Falha ao publicar solicitação de reembolso na fila: {ex.Message}");
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

    }
}
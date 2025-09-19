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
            //foreach (var game in commandGames)
            //{
            //    game.HistoryPaymentId = historyPayment.Id;
            //}

            //var purchaseCommand = new CreatePurchaseCommand(request.UserId, commandGames);
            //var queueUrl = _configuration["SqsPublisher:QueueUrl"];

            //await _queuePublisher.PublishAsync(queueUrl, purchaseCommand, ct);

            return historyPayment;
        }

        public async Task<HistoryPayment?> CreateRefundAsync(RefundRequest request, CancellationToken ct = default)
        {
            // 1. Validar se o jogo existe na biblioteca do usuário
            var userLibrary = await _userLibraryRepository.FirstOrDefaultAsync(
                predicate: x => x.UserId == request.UserId,
                trackChanges: true, // Habilitamos o tracking para salvar as mudanças
                includes: x => x.Items
            );

            if (userLibrary == null || userLibrary.Items.All(i => i.GameId != request.GameId))
            {
                Notify("O jogo solicitado não foi encontrado na biblioteca do usuário.");
                return null;
            }

            // 2. Retirar o jogo da biblioteca
            // Reutilizamos o serviço que já tem essa responsabilidade
            var removalResult = await _userLibraryService.DeleteGameForUser(request.UserId, request.GameId, ct);
            if (removalResult != true)
            {
                // Se a remoção falhar, o Notifier do UserLibraryService já terá a mensagem de erro
                return null;
            }

            // 3. Salvar no histórico de pagamentos
            var historyPayment = new HistoryPayment
            {
                Status = StatusTransaction.Finished, // Podemos finalizar, pois a ação principal já ocorreu
                Type = TransactionType.Refund,
                TransactionGames = new List<TransactionGame>
                {
                    new TransactionGame { GameId = request.GameId }
                }
            };

            // Associa o ID do histórico ao jogo transacionado
            foreach (var game in historyPayment.TransactionGames)
            {
                game.HistoryPaymentId = historyPayment.Id;
            }

            // O Unit of Work garante que as duas operações (remover da biblioteca e adicionar ao histórico)
            // sejam salvas na mesma transação de banco de dados.
            await _historyPaymentRepository.AddAsync(historyPayment, ct);

            // O commit aqui salva tanto a remoção do jogo (feita pelo _userLibraryService)
            // quanto a adição do novo histórico de pagamento.
            var success = await _unitOfWork.CommitAsync(ct);

            if (!success)
            {
                Notify("Ocorreu um erro ao salvar o histórico de devolução.");
                return null;
            }

            return historyPayment;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
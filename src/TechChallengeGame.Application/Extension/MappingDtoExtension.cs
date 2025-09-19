using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Entities.Enums;
using TechChallengeGame.Shared.Models.Dtos.Requests;
using TechChallengeGame.Shared.Models.Dtos.Responses;

namespace TecChallenge.Application.Extensions;

public static class MappingDtoExtension
{
    public static GameResponse MapToDto(this Game game)
    {
        return new GameResponse
        {
            Id = game.Id,
            Name = game.Name,
            Price = game.Price,
            Genre = game.Genre,
            IsActive = game.IsActive,
            CreatedAt = game.CreatedAt,
            UpdatedAt = game.UpdatedAt
        };
    }
    public static UserLibraryResponse MapToDto(this UserLibrary userLibrary)
    {
        return new UserLibraryResponse
        {
            Id = userLibrary.Id,
            UserId = userLibrary.UserId,
            Items = userLibrary.Items.Select(x => x.MapToDto())
        };
    }

    private static LibraryItemReponse MapToDto(this LibraryItem item)
    {
        return new LibraryItemReponse
        {
            Id = item.Id,
            GameId = item.GameId,
            UserLibraryId = item.UserLibraryId,
            PurchasedAt = item.PurchasedAt,
            PurchasePrice = item.PurchasePrice
        };
    }

    public static PromotionResponse MapToDto(this Promotion promotion)
    {
        return new PromotionResponse
        {
            Id = promotion.Id,
            Name = promotion.Name,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            GamesOnSale = promotion.GamesOnSale.Select(x => x.MapToDto())
        };
    }

    public static PromotionGameResponse MapToDto(this PromotionGame promotionGame)
    {
        return new PromotionGameResponse
        {
            Id = promotionGame.Id,
            PromotionId = promotionGame.PromotionId,
            GameId = promotionGame.GameId,
            DiscountPercentage = promotionGame.DiscountPercentage
        };
    }


    public static HistoryPaymentResponse MapToDto(this HistoryPayment historyPayment)
    {
        return new HistoryPaymentResponse
        {
            Id = historyPayment.Id,
            PaymentTransactionId = historyPayment.PaymentTransactionId,
            Status = historyPayment.Status.ToString(),
            Type = historyPayment.Type.ToString(),
            CreatedAt = historyPayment.CreatedAt,
            UpdatedAt = historyPayment.UpdatedAt,
            TransactionGames = historyPayment.TransactionGames.Select(tg => tg.MapToDto()).ToList()
        };
    }

    public static TransactionGameResponse MapToDto(this TransactionGame transactionGame)
    {
        return new TransactionGameResponse
        {
            HistoryPaymentId = transactionGame.HistoryPaymentId,
            GameId = transactionGame.GameId,
            PromotionId = transactionGame.PromotionId
        };
    }

    public static HistoryPayment MapToEntity(this PurchaseRequest request)
    {
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

        foreach (var game in historyPayment.TransactionGames)
        {
            game.HistoryPaymentId = historyPayment.Id;
        }

        return historyPayment;
    }


}
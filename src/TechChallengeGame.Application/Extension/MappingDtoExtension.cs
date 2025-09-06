using TechChallengeGame.Domain.Entities;
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

}
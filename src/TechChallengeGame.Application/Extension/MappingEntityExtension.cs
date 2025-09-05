using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Shared.Models.Dtos.Requests;

namespace TecChallenge.Application.Extensions;

public static class MappingEntityExtension
{
    public static Game MapToEntity(this GameAddRequest game)
    {
        return new Game
        {
            Name = game.Name,
            Price = game.Price
        };
    }

    public static Game MapToEntity(this GameUpdateRequest game)
    {
        return new Game
        {
            Name = game.Name,
            Price = game.Price,
            IsActive = game.IsActive
        };
    }
   
}
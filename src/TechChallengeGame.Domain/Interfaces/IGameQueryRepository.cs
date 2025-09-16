using TechChallengeGame.Shared.Models.Dtos.Responses;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface IGameQueryRepository
    {
        Task<GameResponse> FindByIdAsync(Guid id);
        Task<IEnumerable<GameResponse>> SearchAsync(string query);
        Task<IEnumerable<GameResponse>> GetRecommendationsAsync(Guid gameId);
        Task<object> GetGenreAggregationsAsync();
    }
}
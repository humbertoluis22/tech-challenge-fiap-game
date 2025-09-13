using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Domain.Interfaces;

public interface IPromotionRepository : IRepository<Promotion>
{
    Task<PromotionGame?> GetPromotionGameById(Guid promotionGameId);
}
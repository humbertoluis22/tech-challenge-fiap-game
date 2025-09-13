using Microsoft.EntityFrameworkCore;
using TechChallengeGame.Data.Contexts;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;
    
namespace TechChallengeGame.Data.Repositories;

public class PromotionRepository(AppDbContext context) : Repository<Promotion>(context), IPromotionRepository
{
    public async Task<PromotionGame?> GetPromotionGameById(Guid promotionGameId)
    {
        return await context.PromotionGames.FirstOrDefaultAsync(pg => pg.Id == promotionGameId);
    }
}
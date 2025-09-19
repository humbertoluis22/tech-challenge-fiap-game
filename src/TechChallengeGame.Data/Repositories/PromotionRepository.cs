using Microsoft.EntityFrameworkCore;
using TechChallengeGame.Data.Contexts;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;
    
namespace TechChallengeGame.Data.Repositories;

public class PromotionRepository(AppDbContext context) : Repository<Promotion>(context), IPromotionRepository
{
    public async Task<IReadOnlyList<Promotion>> GetAllWithGamesAsync()
    {
            return await context.Promotions
                 .AsNoTracking()
                 .Include(p => p.GamesOnSale) 
                 .ToListAsync();
    }

    public async Task<PromotionGame?> GetPromotionGameById(Guid promotionGameId)
    {
        return await context.PromotionGames.FirstOrDefaultAsync(pg => pg.Id == promotionGameId);
    }

    public async Task<PromotionGame?> FindByPromotionAndGameAsync(Guid promotionId, Guid gameId)
    {
        return await context.PromotionGames
            .AsNoTracking()
            .FirstOrDefaultAsync(pg => pg.PromotionId == promotionId && pg.GameId == gameId);
    }

}
using TechChallengeGame.Data.Contexts;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;

namespace TechChallengeGame.Data.Repositories;

public class PromotionGameRepository(AppDbContext context)
    : Repository<PromotionGame>(context), IPromotionGameRepository;
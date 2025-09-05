using TechChallengeGame.Data.Contexts;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;

namespace TechChallengeGame.Data.Repositories
{
    public class GameRepository(AppDbContext context)
    : Repository<Game>(context),
        IGameRepository;
}

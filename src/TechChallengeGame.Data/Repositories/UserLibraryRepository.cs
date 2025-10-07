using TechChallengeGame.Data.Contexts;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;

namespace TechChallengeGame.Data.Repositories
{
    public class UserLibraryRepository(AppDbContext context)
    : Repository<UserLibrary>(context),
        IUserLibraryRepository
    {
        public void RemoveLibraryItem(LibraryItem item, CancellationToken ct = default)
        {
            context.LibraryItems.Remove(item);
        }
    }
}

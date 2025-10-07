using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface IUserLibraryRepository : IRepository<UserLibrary>
    {
        void RemoveLibraryItem(LibraryItem item, CancellationToken ct = default);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface IUserLibraryService : IDisposable
    {
        Task<bool> AddAsync(UserLibrary model, CancellationToken ct = default);
        Task<bool?> AddGameForUser(Guid userid, Guid gameId,CancellationToken ct = default);
        Task<bool?> AddGameToLibraryFromEventAsync(Guid userId, Guid gameId, CancellationToken ct = default);
        Task<bool?> DeleteGameForUser(Guid userid, Guid gameId,CancellationToken ct = default);

    }
}

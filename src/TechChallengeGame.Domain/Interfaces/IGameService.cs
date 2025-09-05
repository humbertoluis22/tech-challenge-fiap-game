using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface IGameService : IDisposable
    {
        Task<bool> AddAsync(Game model, CancellationToken ct = default);
        Task<bool?> UpdateAsync(Guid id, Game model, CancellationToken ct = default);
        Task<bool?> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}

using Microsoft.EntityFrameworkCore.Storage;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken ct = default);
        Task<bool> CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }
}

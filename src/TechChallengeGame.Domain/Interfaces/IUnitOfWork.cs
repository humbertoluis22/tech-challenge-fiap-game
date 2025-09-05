using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken ct = default);
        Task<bool> CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }
}

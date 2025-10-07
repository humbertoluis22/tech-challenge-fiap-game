using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Shared.Models.Dtos.Requests;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface ITransactionService : IDisposable
    {
        Task<HistoryPayment> CreatePurchaseAsync(string userId,PurchaseRequest request, CancellationToken ct = default);
        Task<bool> CreateRefundAsync(string userId,RefundRequest request, CancellationToken ct = default);


    }
}

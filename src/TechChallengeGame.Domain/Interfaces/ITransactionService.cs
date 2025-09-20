using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Shared.Models.Dtos.Requests;

namespace TechChallengeGame.Domain.Interfaces
{
    public interface ITransactionService : IDisposable
    {
        Task<HistoryPayment> CreatePurchaseAsync(PurchaseRequest request, CancellationToken ct = default);
        Task<bool> CreateRefundAsync(RefundRequest request, CancellationToken ct = default);


    }
}

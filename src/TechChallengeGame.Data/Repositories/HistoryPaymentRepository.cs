using TechChallengeGame.Data.Contexts;
using TechChallengeGame.Domain.Entities;
using TechChallengeGame.Domain.Interfaces;

namespace TechChallengeGame.Data.Repositories
{
    public class HistoryPaymentRepository : Repository<HistoryPayment>, IHistoryPaymentRepository
    {
        public HistoryPaymentRepository(AppDbContext context) : base(context)
        {
        }
    }
}

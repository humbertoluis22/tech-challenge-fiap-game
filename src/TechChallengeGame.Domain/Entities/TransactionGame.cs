using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Domain.Entities
{
    public class TransactionGame
    {
        public Guid HistoryPaymentId { get; set; }
        public Guid GameId { get; set; }
        public Guid? PromotionId { get; set; }

        // EF Core relationships
        public HistoryPayment HistoryPayment { get; set; }
        public Game Game { get; set; }
        public Promotion Promotion { get; set; }
    }
}

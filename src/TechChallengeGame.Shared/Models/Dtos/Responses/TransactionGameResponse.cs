using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Responses
{
    public class TransactionGameResponse
    {
        public Guid HistoryPaymentId { get; set; }
        public Guid GameId { get; set; }
        public Guid? PromotionId { get; set; }
    }
}

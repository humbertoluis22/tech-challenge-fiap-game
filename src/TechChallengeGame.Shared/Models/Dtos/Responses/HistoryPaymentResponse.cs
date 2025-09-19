using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Responses
{
    public class HistoryPaymentResponse
    {
        public Guid Id { get; set; }
        public Guid? PaymentTransactionId { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<TransactionGameResponse> TransactionGames { get; set; }
    }
}

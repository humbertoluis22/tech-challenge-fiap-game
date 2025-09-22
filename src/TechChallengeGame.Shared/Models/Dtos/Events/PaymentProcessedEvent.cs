using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Events
{
    public class PaymentProcessedEvent
    {
        public Guid UserId { get; set; }
        public Guid PaymentTransactionId { get; set; }
        public List<GameDetail> Games { get; set; } = [];
    }
}

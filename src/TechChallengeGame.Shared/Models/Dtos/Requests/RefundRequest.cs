using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Requests
{
    public class RefundRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid PaymentTransactionId  { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace TechChallengeGame.Shared.Models.Dtos.Requests
{
    public class RefundRequest
    {
 
        [Required]
        public Guid PaymentTransactionId  { get; set; }
    }
}

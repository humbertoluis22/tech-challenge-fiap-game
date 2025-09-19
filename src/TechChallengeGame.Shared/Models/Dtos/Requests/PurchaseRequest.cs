using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Requests
{
    public class PurchaseRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public List<GamePromotionRequest> Games { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Requests
{
    public class GamePromotionRequest
    {
        [Required]
        public Guid GameId { get; set; }
        public Guid? PromotionGameId { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Commands
{
    public record CreatePurchaseCommand(
        [Required] Guid UserId,
        [Required] List<GamePromotion> Games,
        string CommandType = "create-purchase" 

    );
}

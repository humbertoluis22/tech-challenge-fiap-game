using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechChallengeGame.Shared.Models.Dtos.Commands
{
    public record RefundRequestedEvent(
        [Required] Guid UserId,
        [Required] Guid PaymentTransactionId,
        string CommandType = "create-refund" 

    );
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechChallengeGame.Shared.Models.Dtos.Requests;

public class PromotionAddRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required] public DateTime StartDate { get; set; }

    [Required] public DateTime EndDate { get; set; }

    [Required] public IEnumerable<PromotionGameAddRequest> GamesOnSale { get; set; } = [];
}
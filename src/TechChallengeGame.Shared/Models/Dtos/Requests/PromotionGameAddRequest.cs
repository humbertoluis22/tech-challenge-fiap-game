using System;
using System.ComponentModel.DataAnnotations;

namespace TechChallengeGame.Shared.Models.Dtos.Requests;

public class PromotionGameAddRequest
{
    [Required]
    public Guid GameId { get; set; }
    
    [Required]
    [Range(1, 100)]
    public decimal DiscountPercentage { get; set; }
}
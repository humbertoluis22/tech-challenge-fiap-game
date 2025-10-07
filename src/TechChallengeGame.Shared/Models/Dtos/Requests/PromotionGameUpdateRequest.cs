using System.ComponentModel.DataAnnotations;

namespace TechChallengeGame.Shared.Models.Dtos.Requests;

public class PromotionGameUpdateRequest
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid PromotionId { get; set; }
    
    [Required]
    public Guid GameId { get; set; }
    
    [Required]
    [Range(1, 100)]
    public decimal DiscountPercentage { get; set; }
}
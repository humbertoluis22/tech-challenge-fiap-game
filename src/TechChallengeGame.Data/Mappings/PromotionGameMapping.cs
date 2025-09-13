using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Data.Mappings;

public class PromotionGameMapping : IEntityTypeConfiguration<PromotionGame>
{
    public void Configure(EntityTypeBuilder<PromotionGame> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(p => p.PromotionId).IsRequired();

        builder.Property(p => p.GameId).IsRequired();

        builder.Property(p => p.DiscountPercentage).HasColumnType("decimal(5,2)")
            .HasPrecision(5, 2);


        builder.ToTable("PromotionGame");
    }
}
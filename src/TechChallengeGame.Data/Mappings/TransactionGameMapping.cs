using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Data.Mappings
{
    public class TransactionGameMapping : IEntityTypeConfiguration<TransactionGame>
    {
        public void Configure(EntityTypeBuilder<TransactionGame> builder)
        {
            builder.HasKey(t => new { t.HistoryPaymentId, t.GameId });

            builder.HasOne(t => t.HistoryPayment)
                .WithMany(h => h.TransactionGames)
                .HasForeignKey(t => t.HistoryPaymentId);

            builder.HasOne(t => t.Game)
                .WithMany()
                .HasForeignKey(t => t.GameId);

            builder.HasOne(t => t.Promotion)
                .WithMany()
                .HasForeignKey(t => t.PromotionId)
                .IsRequired(false);

            builder.ToTable("TransactionGame");
        }
    }
}

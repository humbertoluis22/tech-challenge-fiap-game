using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Data.Mappings
{
    public class HistoryPaymentMapping : IEntityTypeConfiguration<HistoryPayment>
    {
        public void Configure(EntityTypeBuilder<HistoryPayment> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(p => p.PaymentTransactionId);
            builder.Property(p => p.Status).IsRequired();
            builder.Property(p => p.Type).IsRequired();
            builder.Property(p => p.CreatedAt).IsRequired();
            builder.Property(p => p.UpdatedAt);

            builder.HasMany(p => p.TransactionGames)
                .WithOne(g => g.HistoryPayment)
                .HasForeignKey(g => g.HistoryPaymentId);

            builder.ToTable("HistoryPayment");
        }
    }
}

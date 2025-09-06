using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Data.Mappings
{
    public class UserLibraryMapping : IEntityTypeConfiguration<UserLibrary>
    {
        public void Configure(EntityTypeBuilder<UserLibrary> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(p => p.UserId).IsRequired();

            builder.HasMany(p => p.Items)
                .WithOne(l => l.UserLibrary)
                .HasForeignKey(l => l.UserLibraryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("UserLibrary");
        }
    }
}

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TechChallengeGame.Domain.Entities;

namespace TechChallengeGame.Data.Mappings
{
    public class LogMapping : IEntityTypeConfiguration<Log>
    {
        public void Configure(EntityTypeBuilder<Log> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(c => c.ApplicationName).HasColumnType("text");

            builder.Property(c => c.Message).HasColumnType("text");

            builder.Property(c => c.MessageTemplate).HasColumnType("text");

            builder.Property(c => c.Level).HasColumnType("varchar(128)");

            builder.Property(c => c.Exception).HasColumnType("text");

            builder.ToTable("Log");
        }
    }
}

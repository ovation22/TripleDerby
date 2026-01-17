using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> builder)
    {
        builder.Property(ts => ts.Result)
            .HasMaxLength(1000)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(ts => ts.HorseId);
        builder.HasIndex(ts => ts.SessionDate);
    }
}

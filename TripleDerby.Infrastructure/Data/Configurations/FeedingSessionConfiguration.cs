using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class FeedingSessionConfiguration : IEntityTypeConfiguration<FeedingSession>
{
    public void Configure(EntityTypeBuilder<FeedingSession> builder)
    {
        builder.Property(fs => fs.Result)
            .HasConversion<byte>();

        // Indexes for common queries
        builder.HasIndex(fs => fs.HorseId);
        builder.HasIndex(fs => fs.SessionDate);
    }
}

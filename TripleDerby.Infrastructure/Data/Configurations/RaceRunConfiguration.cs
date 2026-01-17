using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class RaceRunConfiguration : IEntityTypeConfiguration<RaceRun>
{
    public void Configure(EntityTypeBuilder<RaceRun> builder)
    {
        builder.HasOne(x => x.Race)
            .WithMany()
            .HasForeignKey(x => x.RaceId);

        builder.Property(rr => rr.ConditionId)
            .HasConversion<byte>();

        // Indexes for common queries
        builder.HasIndex(rr => rr.CreatedDate);
        builder.HasIndex(rr => rr.RaceId);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class RaceRunHorseConfiguration : IEntityTypeConfiguration<RaceRunHorse>
{
    public void Configure(EntityTypeBuilder<RaceRunHorse> builder)
    {
        builder.Property(rrh => rrh.Distance)
            .HasPrecision(5, 2);

        // Indexes for common queries
        builder.HasIndex(rrh => rrh.RaceRunId);
        builder.HasIndex(rrh => rrh.HorseId);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class RaceRunTickConfiguration : IEntityTypeConfiguration<RaceRunTick>
{
    public void Configure(EntityTypeBuilder<RaceRunTick> builder)
    {
        builder.Property(rrt => rrt.Note)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(rrt => rrt.RaceRunId);
    }
}

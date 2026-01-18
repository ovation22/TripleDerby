using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class RaceRunTickHorseConfiguration : IEntityTypeConfiguration<RaceRunTickHorse>
{
    public void Configure(EntityTypeBuilder<RaceRunTickHorse> builder)
    {
        builder.Property(rrth => rrth.Distance)
            .HasPrecision(5, 2);

        builder.HasOne(x => x.RaceRunTick)
            .WithMany(x => x.RaceRunTickHorses)
            .HasForeignKey(x => x.RaceRunTickId);

        builder.HasIndex(rrth => rrth.RaceRunTickId);
    }
}

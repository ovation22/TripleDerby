using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class HorseStatisticConfiguration : IEntityTypeConfiguration<HorseStatistic>
{
    public void Configure(EntityTypeBuilder<HorseStatistic> builder)
    {
        builder.HasKey(hs => new { hs.HorseId, hs.StatisticId });

        builder.Property(c => c.StatisticId)
            .HasConversion<byte>();

        // Ignore computed properties
        builder.Ignore(h => h.Speed);
        builder.Ignore(h => h.Stamina);
        builder.Ignore(h => h.Agility);
        builder.Ignore(h => h.Durability);
        builder.Ignore(h => h.Happiness);
    }
}

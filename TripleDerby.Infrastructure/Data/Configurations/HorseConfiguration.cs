using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class HorseConfiguration : IEntityTypeConfiguration<Horse>
{
    public void Configure(EntityTypeBuilder<Horse> builder)
    {
        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.LegTypeId)
            .HasConversion<byte>();

        builder.HasOne(x => x.Sire)
            .WithMany()
            .HasForeignKey(x => x.SireId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Dam)
            .WithMany()
            .HasForeignKey(x => x.DamId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed properties (from HorseStatistic collection)
        builder.Ignore(h => h.Speed);
        builder.Ignore(h => h.Stamina);
        builder.Ignore(h => h.Agility);
        builder.Ignore(h => h.Durability);
        builder.Ignore(h => h.Happiness);

        // Indexes for common queries
        builder.HasIndex(h => h.OwnerId);
        builder.HasIndex(h => h.Name);
    }
}

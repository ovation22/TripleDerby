using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class RaceConfiguration : IEntityTypeConfiguration<Race>
{
    public void Configure(EntityTypeBuilder<Race> builder)
    {
        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.Furlongs)
            .HasPrecision(5, 2);

        builder.Property(r => r.TrackId)
            .HasConversion<byte>();

        builder.Property(r => r.SurfaceId)
            .HasConversion<byte>();

        builder.Property(r => r.RaceClassId)
            .HasConversion<byte>();
    }
}

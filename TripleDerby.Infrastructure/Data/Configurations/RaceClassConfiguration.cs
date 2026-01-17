using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class RaceClassConfiguration : IEntityTypeConfiguration<RaceClass>
{
    public void Configure(EntityTypeBuilder<RaceClass> builder)
    {
        builder.Property(rc => rc.Id)
            .HasConversion<byte>();

        builder.Property(rc => rc.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(rc => rc.Description)
            .HasMaxLength(500)
            .IsRequired();
    }
}

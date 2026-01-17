using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class SurfaceConfiguration : IEntityTypeConfiguration<Surface>
{
    public void Configure(EntityTypeBuilder<Surface> builder)
    {
        builder.Property(s => s.Id)
            .HasConversion<byte>();

        builder.Property(s => s.Name)
            .HasMaxLength(50)
            .IsRequired();
    }
}

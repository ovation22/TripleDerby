using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> builder)
    {
        builder.Property(c => c.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(200)
            .IsRequired();
    }
}

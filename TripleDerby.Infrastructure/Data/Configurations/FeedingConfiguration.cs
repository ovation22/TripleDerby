using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class FeedingConfiguration : IEntityTypeConfiguration<Feeding>
{
    public void Configure(EntityTypeBuilder<Feeding> builder)
    {
        builder.Property(f => f.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(f => f.CategoryId)
            .HasConversion<byte>();
    }
}

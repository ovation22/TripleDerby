using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class FeedingCategoryConfiguration : IEntityTypeConfiguration<FeedingCategory>
{
    public void Configure(EntityTypeBuilder<FeedingCategory> builder)
    {
        builder.Property(fc => fc.Id)
            .HasConversion<byte>();

        builder.Property(fc => fc.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(fc => fc.Description)
            .HasMaxLength(500)
            .IsRequired();
    }
}

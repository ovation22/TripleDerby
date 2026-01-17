using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class LegTypeConfiguration : IEntityTypeConfiguration<LegType>
{
    public void Configure(EntityTypeBuilder<LegType> builder)
    {
        builder.Property(lt => lt.Id)
            .HasConversion<byte>();

        builder.Property(lt => lt.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(lt => lt.Description)
            .HasMaxLength(200)
            .IsRequired();
    }
}

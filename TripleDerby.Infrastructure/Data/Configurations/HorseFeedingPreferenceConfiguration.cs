using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class HorseFeedingPreferenceConfiguration : IEntityTypeConfiguration<HorseFeedingPreference>
{
    public void Configure(EntityTypeBuilder<HorseFeedingPreference> builder)
    {
        builder.Property(hfp => hfp.Preference)
            .HasConversion<byte>();

        builder.HasIndex(e => new { e.HorseId, e.FeedingId })
            .IsUnique();
    }
}

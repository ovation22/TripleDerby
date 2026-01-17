using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class FeedingRequestConfiguration : IEntityTypeConfiguration<FeedingRequest>
{
    public void Configure(EntityTypeBuilder<FeedingRequest> builder)
    {
        // Add any specific configuration for FeedingRequest if needed
        // Currently uses default conventions
    }
}

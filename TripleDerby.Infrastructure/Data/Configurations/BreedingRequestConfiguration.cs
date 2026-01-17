using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class BreedingRequestConfiguration : IEntityTypeConfiguration<BreedingRequest>
{
    public void Configure(EntityTypeBuilder<BreedingRequest> builder)
    {
        builder.ToTable("BreedingRequests", schema: "brd");

        builder.Property(br => br.Status)
            .HasConversion<byte>()
            .HasDefaultValue(BreedingRequestStatus.Pending)
            .IsRequired();

        builder.Property(br => br.FailureReason)
            .HasMaxLength(1024);

        // Indexes for common queries
        builder.HasIndex(br => br.Status);
        builder.HasIndex(br => br.CreatedDate);
    }
}

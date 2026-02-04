using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class FeedingRequestConfiguration : IEntityTypeConfiguration<FeedingRequest>
{
    public void Configure(EntityTypeBuilder<FeedingRequest> builder)
    {
        builder.ToTable("FeedingRequests", schema: "fed");

        builder.Property(fr => fr.Status)
            .HasConversion<byte>()
            .HasDefaultValue(FeedingRequestStatus.Pending)
            .IsRequired();

        builder.Property(fr => fr.FailureReason)
            .HasMaxLength(1024);

        builder.Property(fr => fr.HorseId)
            .IsRequired();

        builder.Property(fr => fr.FeedingId)
            .IsRequired();

        builder.Property(fr => fr.OwnerId)
            .IsRequired();

        builder.Property(fr => fr.CreatedDate)
            .IsRequired();

        builder.Property(fr => fr.CreatedBy)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(fr => fr.Status);
        builder.HasIndex(fr => fr.CreatedDate);
        builder.HasIndex(fr => fr.HorseId);
    }
}

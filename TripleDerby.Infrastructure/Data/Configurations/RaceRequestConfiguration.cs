using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class RaceRequestConfiguration : IEntityTypeConfiguration<RaceRequest>
{
    public void Configure(EntityTypeBuilder<RaceRequest> builder)
    {
        builder.ToTable("RaceRequests", schema: "rac");

        builder.Property(rr => rr.Status)
            .HasConversion<byte>()
            .HasDefaultValue(RaceRequestStatus.Pending)
            .IsRequired();

        builder.Property(rr => rr.FailureReason)
            .HasMaxLength(1024);

        builder.Property(rr => rr.RaceId)
            .IsRequired();

        builder.Property(rr => rr.HorseId)
            .IsRequired();

        builder.Property(rr => rr.OwnerId)
            .IsRequired();

        builder.Property(rr => rr.CreatedDate)
            .IsRequired();

        builder.Property(rr => rr.CreatedBy)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(rr => rr.Status);
        builder.HasIndex(rr => rr.CreatedDate);
        builder.HasIndex(rr => rr.HorseId);
        builder.HasIndex(rr => rr.OwnerId);
    }
}

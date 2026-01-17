using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Infrastructure.Data.Configurations;

public class TrainingRequestConfiguration : IEntityTypeConfiguration<TrainingRequest>
{
    public void Configure(EntityTypeBuilder<TrainingRequest> builder)
    {
        builder.ToTable("TrainingRequests", schema: "trn");

        builder.Property(tr => tr.Status)
            .HasConversion<byte>()
            .HasDefaultValue(TrainingRequestStatus.Pending)
            .IsRequired();

        builder.Property(tr => tr.FailureReason)
            .HasMaxLength(1024);

        builder.Property(tr => tr.HorseId)
            .IsRequired();

        builder.Property(tr => tr.TrainingId)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(tr => tr.Status);
        builder.HasIndex(tr => tr.CreatedDate);
        builder.HasIndex(tr => tr.HorseId);
    }
}

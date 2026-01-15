using Microsoft.EntityFrameworkCore;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Infrastructure.Data;

public class TripleDerbyContext(DbContextOptions<TripleDerbyContext> options) : DbContext(options)
{
    public virtual DbSet<Color> Colors { get; set; } = null!;
    public virtual DbSet<Condition> Conditions { get; set; } = null!;
    public virtual DbSet<Feeding> Feedings { get; set; } = null!;
    public virtual DbSet<FeedingCategory> FeedingCategories { get; set; } = null!;
    public virtual DbSet<FeedingSession> FeedingSession { set; get; } = null!;
    public virtual DbSet<HorseFeedingPreference> HorseFeedingPreferences { get; set; } = null!;
    public virtual DbSet<Horse> Horses { get; set; } = null!;
    public virtual DbSet<HorseStatistic> HorseStatistics { get; set; } = null!;
    public virtual DbSet<LegType> LegTypes { get; set; } = null!;
    public virtual DbSet<Race> Races { get; set; } = null!;
    public virtual DbSet<RaceClass> RaceClasses { get; set; } = null!;
    public virtual DbSet<RaceRun> RaceRuns { get; set; } = null!;
    public virtual DbSet<RaceRunHorse> RaceRunHorses { get; set; } = null!;
    public virtual DbSet<RaceRunTick> RaceRunTicks { get; set; } = null!;
    public virtual DbSet<RaceRunTickHorse> RaceRunTickHorses { get; set; } = null!;
    public virtual DbSet<Statistic> Statistics { get; set; } = null!;
    public virtual DbSet<Surface> Surfaces { get; set; } = null!;
    public virtual DbSet<Track> Tracks { get; set; } = null!;
    public virtual DbSet<Training> Trainings { get; set; } = null!;
    public virtual DbSet<TrainingSession> TrainingSessions { get; set; } = null!;
    public virtual DbSet<BreedingRequest> BreedingRequests { get; set; } = null!;
    public virtual DbSet<FeedingRequest> FeedingRequests { get; set; } = null!;
    public virtual DbSet<RaceRequest> RaceRequests { get; set; } = null!;
    public virtual DbSet<TrainingRequest> TrainingRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Seed();

        modelBuilder.Entity<HorseStatistic>()
            .Property(c => c.StatisticId)
            .HasConversion<byte>();

        modelBuilder.Entity<HorseStatistic>()
             .HasKey(hs => new { hs.HorseId, hs.StatisticId });


        modelBuilder.Entity<HorseStatistic>()
            .Ignore(h => h.Speed)
            .Ignore(h => h.Stamina)
            .Ignore(h => h.Agility)
            .Ignore(h => h.Durability)
            .Ignore(h => h.Happiness);

        modelBuilder.Entity<Horse>()
            .Property(c => c.LegTypeId)
            .HasConversion<byte>();

        modelBuilder.Entity<Horse>()
            .HasOne(x => x.Sire)
            .WithMany();

        modelBuilder.Entity<Horse>()
            .HasOne(x => x.Dam)
            .WithMany();

        modelBuilder.Entity<Horse>()
            .Ignore(h => h.Speed)
            .Ignore(h => h.Stamina)
            .Ignore(h => h.Agility)
            .Ignore(h => h.Durability)
            .Ignore(h => h.Happiness);

        modelBuilder.Entity<LegType>()
            .Property(c => c.Id)
            .HasConversion<byte>();

        modelBuilder.Entity<Track>()
            .Property(c => c.Id)
            .HasConversion<byte>();

        modelBuilder.Entity<Statistic>()
            .Property(c => c.Id)
            .HasConversion<byte>();

        modelBuilder.Entity<Surface>()
            .Property(c => c.Id)
            .HasConversion<byte>();

        modelBuilder.Entity<Race>()
            .Property(c => c.Furlongs)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Race>()
            .Property(c => c.TrackId)
            .HasConversion<byte>();

        modelBuilder.Entity<Race>()
            .Property(c => c.SurfaceId)
            .HasConversion<byte>();

        modelBuilder.Entity<Race>()
            .Property(c => c.RaceClassId)
            .HasConversion<byte>();

        modelBuilder.Entity<RaceClass>()
            .Property(c => c.Id)
            .HasConversion<byte>();

        modelBuilder.Entity<FeedingCategory>()
            .Property(c => c.Id)
            .HasConversion<byte>();

        modelBuilder.Entity<Feeding>()
            .Property(c => c.CategoryId)
            .HasConversion<byte>();

        modelBuilder.Entity<FeedingSession>()
            .Property(c => c.Result)
            .HasConversion<byte>();

        // HorseFeedingPreference configuration (Feature 022 - Horse Feeding System)
        modelBuilder.Entity<HorseFeedingPreference>()
            .Property(c => c.Preference)
            .HasConversion<byte>();

        modelBuilder.Entity<HorseFeedingPreference>()
            .HasIndex(e => new { e.HorseId, e.FeedingId })
            .IsUnique();

        modelBuilder.Entity<RaceRun>()
            .HasOne(x => x.Race);

        modelBuilder.Entity<RaceRun>()
            .Property(c => c.ConditionId)
            .HasConversion<byte>();

        modelBuilder.Entity<RaceRunTickHorse>()
            .HasOne(x => x.RaceRunTick)
            .WithMany(x => x.RaceRunTickHorses)
            .HasForeignKey(x => x.RaceRunTickId);

        modelBuilder.Entity<BreedingRequest>()
            .ToTable("BreedingRequests", schema: "brd")
            .Property(x => x.Status)
            .HasConversion<byte>()
            .HasDefaultValue(BreedingRequestStatus.Pending)
            .IsRequired();

        modelBuilder.Entity<BreedingRequest>()
            .Property(x => x.FailureReason).HasMaxLength(1024);

        // RaceRequest configuration (Feature 011 - Race Microservice Migration)
        // Uses 'rac' schema (mirroring Breeding's 'brd' schema)
        modelBuilder.Entity<RaceRequest>()
            .ToTable("RaceRequests", schema: "rac")
            .HasKey(e => e.Id);

        modelBuilder.Entity<RaceRequest>()
            .Property(e => e.Status)
            .HasConversion<byte>()
            .HasDefaultValue(RaceRequestStatus.Pending)
            .IsRequired();

        modelBuilder.Entity<RaceRequest>()
            .Property(e => e.FailureReason)
            .HasMaxLength(1024);

        modelBuilder.Entity<RaceRequest>()
            .Property(e => e.RaceId).IsRequired();

        // TrainingRequest configuration (Feature 020 - Horse Training System)
        // Uses 'trn' schema (following Breeding/Racing pattern)
        modelBuilder.Entity<TrainingRequest>()
            .ToTable("TrainingRequests", schema: "trn")
            .HasKey(e => e.Id);

        modelBuilder.Entity<TrainingRequest>()
            .Property(e => e.Status)
            .HasConversion<byte>()
            .HasDefaultValue(TrainingRequestStatus.Pending)
            .IsRequired();

        modelBuilder.Entity<TrainingRequest>()
            .Property(e => e.FailureReason)
            .HasMaxLength(1024);

        modelBuilder.Entity<TrainingRequest>()
            .Property(e => e.HorseId).IsRequired();

        modelBuilder.Entity<TrainingRequest>()
            .Property(e => e.TrainingId).IsRequired();

        modelBuilder.Entity<RaceRequest>()
            .Property(e => e.HorseId).IsRequired();

        modelBuilder.Entity<RaceRequest>()
            .Property(e => e.OwnerId).IsRequired();

        modelBuilder.Entity<RaceRequest>()
            .Property(e => e.CreatedDate).IsRequired();

        modelBuilder.Entity<RaceRequest>()
            .Property(e => e.CreatedBy).IsRequired();

        // Indexes for common queries
        modelBuilder.Entity<RaceRequest>()
            .HasIndex(e => e.Status);

        modelBuilder.Entity<RaceRequest>()
            .HasIndex(e => e.CreatedDate);

        modelBuilder.Entity<RaceRequest>()
            .HasIndex(e => e.HorseId);

        modelBuilder.Entity<RaceRequest>()
            .HasIndex(e => e.OwnerId);
    }
}
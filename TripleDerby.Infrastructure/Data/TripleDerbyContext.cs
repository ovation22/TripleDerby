using Microsoft.EntityFrameworkCore;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Infrastructure.Data;

public class TripleDerbyContext(DbContextOptions<TripleDerbyContext> options) : DbContext(options)
{
    public virtual DbSet<Color> Colors { get; set; } = null!;
    public virtual DbSet<Condition> Conditions { get; set; } = null!;
    public virtual DbSet<Feeding> Feedings { get; set; } = null!;
    public virtual DbSet<FeedingSession> FeedingSession { set; get; } = null!;
    public virtual DbSet<Horse> Horses { get; set; } = null!;
    public virtual DbSet<HorseStatistic> HorseStatistics { get; set; } = null!;
    public virtual DbSet<LegType> LegTypes { get; set; } = null!;
    public virtual DbSet<Race> Races { get; set; } = null!;
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

        modelBuilder.Entity<FeedingSession>()
            .Property(c => c.Result)
            .HasConversion<byte>();

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
    }
}
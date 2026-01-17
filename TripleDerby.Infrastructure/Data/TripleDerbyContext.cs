using Microsoft.EntityFrameworkCore;
using TripleDerby.Core.Entities;

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
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TripleDerbyContext).Assembly);

        // Apply seed data
        modelBuilder.Seed();
    }
}
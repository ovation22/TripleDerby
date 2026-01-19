using TripleDerby.Core.Abstractions.Generators;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Services;
using TripleDerby.Core.Specifications;
using TripleDerby.Services.Breeding.Abstractions;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Breeding;

/// <summary>
/// Executes breeding operations to create foals from parent horses.
/// </summary>
public class BreedingExecutor(
    IRandomGenerator randomGenerator,
    ITripleDerbyRepository repository,
    IHorseNameGenerator horseNameGenerator,
    ITimeManager timeManager,
    ILogger<BreedingExecutor> logger,
    ColorCache colorCache) : IBreedingExecutor
{
    public async Task<BreedingResult> Breed(Guid sireId, Guid damId, Guid ownerId, CancellationToken cancellationToken = default)
    {
        // Resolve parents with cancellation
        var dam = await repository.SingleOrDefaultAsync(new ParentHorseSpecification(damId), cancellationToken);
        var sire = await repository.SingleOrDefaultAsync(new ParentHorseSpecification(sireId), cancellationToken);

        if (dam is null)
            throw new InvalidOperationException($"Unable to retrieve Dam ({damId})");

        if (sire is null)
            throw new InvalidOperationException($"Unable to retrieve Sire ({sireId})");

        logger.LogInformation("Starting Breeding for {SireId} and {DamId}", sireId, damId);

        var isMale = GetRandomGender();
        var legTypeId = GetRandomLegType();
        var color = await GetRandomColor(sire.Color.IsSpecial, dam.Color.IsSpecial, true, cancellationToken);
        var statistics = GenerateHorseStatistics(sire.Statistics, dam.Statistics);
        var name = horseNameGenerator.Generate();

        var horse = new Horse
        {
            Name = name,
            ColorId = color.Id,
            LegTypeId = legTypeId,
            IsMale = isMale,
            SireId = sireId,
            DamId = damId,
            RaceStarts = 0,
            RaceWins = 0,
            RacePlace = 0,
            RaceShow = 0,
            Earnings = 0,
            IsRetired = false,
            Parented = 0,
            OwnerId = ownerId,
            Statistics = statistics,
            CreatedBy = ownerId,
            CreatedDate = timeManager.OffsetUtcNow()
        };

        // Create the foal in the database
        var createdFoal = await repository.CreateAsync(horse, cancellationToken);

        return new BreedingResult
        {
            FoalId = createdFoal.Id,
            FoalName = createdFoal.Name,
            SireId = sireId,
            DamId = damId,
            OwnerId = ownerId,
            IsMale = createdFoal.IsMale,
            ColorId = createdFoal.ColorId,
            LegTypeId = createdFoal.LegTypeId
        };
    }

    private bool GetRandomGender()
    {
        return randomGenerator.Next(0, 2) == 1;
    }

    private LegTypeId GetRandomLegType()
    {
        var legTypes = Enum.GetValues(typeof(LegTypeId)).Cast<LegTypeId>().ToList();
        if (legTypes.Count == 0) throw new InvalidOperationException("No leg types defined.");

        var idx = randomGenerator.Next(0, legTypes.Count);
        return legTypes[idx];
    }

    private async Task<Color> GetRandomColor(bool isSireSpecial, bool isDamSpecial, bool includeSpecialColors, CancellationToken cancellationToken)
    {
        // Use ColorCache instead of querying repository directly (performance optimization)
        var colors = await colorCache.GetColorsAsync(repository, cancellationToken);

        var candidates = colors
            .Where(x => includeSpecialColors || !x.IsSpecial)
            .ToList();

        if (!candidates.Any())
            throw new InvalidOperationException("No colors available for selection.");

        // Interpret stored Weight as "rarity" (larger = rarer).
        // Convert to frequency via inversion so small weights (common colors) are more likely.
        double singleParentSpecialMultiplier = 10.0;
        double bothParentsSpecialMultiplier = 50.0;
        double specialMultiplier = 1.0;
        if (isSireSpecial && isDamSpecial) specialMultiplier = bothParentsSpecialMultiplier;
        else if (isSireSpecial || isDamSpecial) specialMultiplier = singleParentSpecialMultiplier;

        var weightList = new List<double>(candidates.Count);
        double totalWeight = 0.0;
        foreach (var c in candidates)
        {
            var denominator = Math.Max(1.0, c.Weight);
            double frequency = 1.0 / denominator;

            if (c.IsSpecial)
                frequency *= specialMultiplier;

            weightList.Add(frequency);
            totalWeight += frequency;
        }

        var n = randomGenerator.Next(0, int.MaxValue);
        double r = (n / (double)int.MaxValue) * totalWeight;

        double acc = 0.0;
        for (int i = 0; i < candidates.Count; i++)
        {
            acc += weightList[i];
            if (r < acc)
                return candidates[i];
        }

        return candidates.Last();
    }

    private List<HorseStatistic> GenerateHorseStatistics(ICollection<HorseStatistic> sireStats, ICollection<HorseStatistic> damStats)
    {
        if (sireStats == null) throw new ArgumentNullException(nameof(sireStats));
        if (damStats == null) throw new ArgumentNullException(nameof(damStats));

        List<HorseStatistic> foalStatistics =
            [new() { StatisticId = StatisticId.Happiness, Actual = 50, DominantPotential = 100 }];

        var requiredStats = Enum.GetValues(typeof(StatisticId)).Cast<StatisticId>().Where(x => x != StatisticId.Happiness).ToList();
        foreach (var stat in requiredStats)
        {
            if (sireStats.All(x => x.StatisticId != stat))
                throw new InvalidOperationException($"Sire is missing statistic {stat}.");
            if (damStats.All(x => x.StatisticId != stat))
                throw new InvalidOperationException($"Dam is missing statistic {stat}.");
        }

        foalStatistics
            .AddRange(requiredStats.Select(statistic => GenerateHorseStatistic(sireStats, damStats, statistic)));

        return foalStatistics;
    }

    private HorseStatistic GenerateHorseStatistic(IEnumerable<HorseStatistic> sireStats, IEnumerable<HorseStatistic> damStats, StatisticId statistic)
    {
        int punnettQuadrant = randomGenerator.Next(1, 5);
        int whichGeneToPick = randomGenerator.Next(1, 3);

        double dominantPotential;
        double recessivePotential;

        HorseStatistic sireStatistic = sireStats.Single(x => x.StatisticId == statistic);
        HorseStatistic damStatistic = damStats.Single(x => x.StatisticId == statistic);

        switch (punnettQuadrant)
        {
            case 1:
                if (whichGeneToPick == 1)
                {
                    dominantPotential = sireStatistic.DominantPotential;
                    recessivePotential = damStatistic.RecessivePotential;
                }
                else
                {
                    dominantPotential = damStatistic.DominantPotential;
                    recessivePotential = sireStatistic.RecessivePotential;
                }

                break;
            case 2:
                dominantPotential = sireStatistic.DominantPotential;
                recessivePotential = damStatistic.RecessivePotential;
                break;
            case 3:
                dominantPotential = damStatistic.DominantPotential;
                recessivePotential = sireStatistic.RecessivePotential;
                break;
            default: //case 4
                if (whichGeneToPick == 1)
                {
                    dominantPotential = sireStatistic.DominantPotential;
                    recessivePotential = damStatistic.RecessivePotential;
                }
                else
                {
                    dominantPotential = damStatistic.DominantPotential;
                    recessivePotential = sireStatistic.RecessivePotential;
                }

                break;
        }

        dominantPotential = MutatePotentialGene(dominantPotential);
        recessivePotential = MutatePotentialGene(recessivePotential);

        int min = Math.Max(1, (int)(dominantPotential / 3.0));
        int maxExclusive = Math.Max(min + 1, (int)(dominantPotential / 2.0) + 1);
        double actual = randomGenerator.Next(min, maxExclusive);

        var foalStatistic = new HorseStatistic
        {
            StatisticId = sireStatistic.StatisticId,
            Actual = actual,
            DominantPotential = dominantPotential,
            RecessivePotential = recessivePotential
        };

        return foalStatistic;
    }

    private double MutatePotentialGene(double potential)
    {
        int mutationMultiplier = randomGenerator.Next(1, 101);
        int mutationLowerBound;
        int mutationUpperBound;

        switch (mutationMultiplier)
        {
            case 1:
                mutationLowerBound = 0;
                mutationUpperBound = 16;
                break;
            case 2:
                mutationLowerBound = -15;
                mutationUpperBound = 1;
                break;
            default:
                mutationLowerBound = -5;
                mutationUpperBound = 6;
                break;
        }

        potential = potential + randomGenerator.Next(mutationLowerBound, mutationUpperBound);

        potential = potential is < 30 or > 95 ? 50 : potential;

        return potential;
    }
}

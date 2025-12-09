using TripleDerby.Core.Abstractions.Generators;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Breeding;

public class BreedingRequestProcessor(
    ILogger<BreedingRequestProcessor> logger,
    IRandomGenerator randomGenerator,
    ITripleDerbyRepository repository,
    IMessagePublisher messagePublisher,
    IHorseNameGenerator horseNameGenerator,
    ITimeManager timeManager)
    : IBreedingRequestProcessor
{
    public async Task ProcessAsync(BreedingRequested request, CancellationToken cancellationToken)
    {
        if (request is null) 
            throw new ArgumentNullException(nameof(request));
        
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Processing breeding request {RequestId} (sire={SireId}, dam={DamId})", request.RequestId, request.SireId, request.DamId);

        // idempotency: load persisted request
        var stored = await repository.FindAsync<BreedingRequest>(request.RequestId, cancellationToken);
        if (stored is null)
        {
            logger.LogWarning("BreedingRequest {RequestId} not found in DB; skipping", request.RequestId);
            return;
        }

        // If already completed, skip
        if (stored.Status == BreedingRequestStatus.Completed)
        {
            logger.LogInformation("Skipping request {RequestId} because status is {Status}", request.RequestId, stored.Status);
            return;
        }

        // If previously failed, allow replay — log and proceed to claim
        if (stored.Status == BreedingRequestStatus.Failed)
        {
            logger.LogInformation("Reprocessing failed BreedingRequest {RequestId}. Previous failure: {FailureReason}", request.RequestId, stored.FailureReason);
        }

        // If already in progress, skip to avoid concurrent processing
        if (stored.Status == BreedingRequestStatus.InProgress)
        {
            logger.LogInformation("Skipping request {RequestId} because it is already InProgress", request.RequestId);
            return;
        }

        // Claim the request so other workers won't process it concurrently.
        try
        {
            stored.Status = BreedingRequestStatus.InProgress;
            stored.UpdatedDate = timeManager.OffsetUtcNow();
            await repository.UpdateAsync(stored, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to claim BreedingRequest {RequestId} for processing; another worker may have claimed it", request.RequestId);
            // reload and re-check status
            stored = await repository.FindAsync<BreedingRequest>(request.RequestId, cancellationToken);
            if (stored is not null && stored.Status != BreedingRequestStatus.InProgress)
            {
                logger.LogInformation("Request {RequestId} is no longer available for processing (status={Status}), skipping", request.RequestId, stored.Status);
                return;
            }

            // if we couldn't claim, and it's still not in progress, proceed — a later duplicate check in DB will avoid double side effects in most cases.
        }

        await Breed(request, cancellationToken);

        logger.LogInformation("Completed processing breeding request {RequestId}", request.RequestId);
    }

    private async Task Breed(BreedingRequested request, CancellationToken cancellationToken)
    {
        // resolve parents with cancellation
        var dam = await repository.SingleOrDefaultAsync(new ParentHorseSpecification(request.DamId), cancellationToken);
        var sire = await repository.SingleOrDefaultAsync(new ParentHorseSpecification(request.SireId), cancellationToken);

        if (dam is null)
            throw new InvalidOperationException($"Unable to retrieve Dam ({request.DamId})");

        if (sire is null)
            throw new InvalidOperationException($"Unable to retrieve Sire ({request.SireId})");

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
            SireId = request.SireId,
            DamId = request.DamId,
            RaceStarts = 0,
            RaceWins = 0,
            RacePlace = 0,
            RaceShow = 0,
            Earnings = 0,
            IsRetired = false,
            Parented = 0,
            OwnerId = request.OwnerId,
            Statistics = statistics,
            CreatedBy = request.OwnerId,
            CreatedDate = timeManager.OffsetUtcNow()
        };

        try
        {
            // Create foal, update parent counters and update BreedingRequest atomically
            var foal = await repository.ExecuteInTransactionAsync(async () =>
            {
                var createdFoal = await repository.CreateAsync(horse, cancellationToken);
                await repository.UpdateParentedAsync(request.SireId, request.DamId, cancellationToken);

                // Update the persisted BreedingRequest with FoalId, status and processed date as part of the same transaction
                var breedingRequestEntity = await repository.FindAsync<BreedingRequest>(request.RequestId, cancellationToken);
                if (breedingRequestEntity != null)
                {
                    breedingRequestEntity.FoalId = createdFoal.Id;
                    breedingRequestEntity.Status = BreedingRequestStatus.Completed;
                    breedingRequestEntity.ProcessedDate = timeManager.OffsetUtcNow();
                    breedingRequestEntity.UpdatedDate = timeManager.OffsetUtcNow();
                    await repository.UpdateAsync(breedingRequestEntity, cancellationToken);
                }

                return createdFoal;
            }, cancellationToken);

            // publish BreedingCompleted AFTER transaction commit
            var completedEvent = new BreedingCompleted(
                request.RequestId,
                request.SireId,
                request.DamId,
                foal.Id,
                request.OwnerId,
                foal.CreatedDate
            );

            try
            {
                await messagePublisher.PublishAsync(completedEvent, cancellationToken: cancellationToken);
            }
            catch (Exception pubEx)
            {
                // Log and persist failure info, but keep Status = Completed so DB reflects foal existence.
                logger.LogError(pubEx, "Failed to publish BreedingCompleted for BreedingId={RequestId}", request.RequestId);
                try
                {
                    var bre = await repository.FindAsync<BreedingRequest>(request.RequestId, cancellationToken);
                    if (bre != null)
                    {
                        // Keep Status = Completed (foal created). Record publish failure details for reconciliation.
                        bre.FailureReason = $"Publish failed: {pubEx.Message}";
                        bre.ProcessedDate = timeManager.OffsetUtcNow();
                        bre.UpdatedDate = timeManager.OffsetUtcNow();
                        await repository.UpdateAsync(bre, cancellationToken);
                    }
                }
                catch (Exception updEx)
                {
                    logger.LogWarning(updEx, "Failed to persist publish-failure metadata for BreedingId={RequestId}", request.RequestId);
                }

                // decide whether to rethrow or swallow — here we rethrow so caller / monitoring sees the failure
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Breeding operation failed for RequestId={RequestId}", request.RequestId);

            try
            {
                var bre = await repository.FindAsync<BreedingRequest>(request.RequestId, cancellationToken);
                if (bre != null)
                {
                    bre.Status = BreedingRequestStatus.Failed;
                    bre.FailureReason = ex.Message;
                    bre.ProcessedDate = timeManager.OffsetUtcNow();
                    bre.UpdatedDate = timeManager.OffsetUtcNow();
                    await repository.UpdateAsync(bre, cancellationToken);
                }
            }
            catch (Exception updEx)
            {
                logger.LogWarning(updEx, "Failed to mark BreedingRequest as failed for RequestId={RequestId}", request.RequestId);
            }

            throw;
        }
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
        var colors = (await repository.GetAllAsync<Color>(cancellationToken)).ToList();

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
            var denom = Math.Max(1.0, (double)c.Weight);
            double frequency = 1.0 / denom;

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
            [new() { StatisticId = StatisticId.Happiness, DominantPotential = 100 }];

        var requiredStats = Enum.GetValues(typeof(StatisticId)).Cast<StatisticId>().Where(x => x != StatisticId.Happiness);
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

        byte dominantPotential;
        byte recessivePotential;

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

        int min = Math.Max(1, dominantPotential / 3);
        int maxExclusive = Math.Max(min + 1, dominantPotential / 2 + 1);
        byte actual = (byte)randomGenerator.Next(min, maxExclusive);

        var foalStatistic = new HorseStatistic
        {
            StatisticId = sireStatistic.StatisticId,
            Actual = actual,
            DominantPotential = dominantPotential,
            RecessivePotential = recessivePotential
        };
        return foalStatistic;
    }

    private byte MutatePotentialGene(byte potential)
    {
        byte mutationMultiplier = (byte)randomGenerator.Next(1, 101);
        int mutationLowerBound;
        byte mutationUpperBound;

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

        potential = (byte)(potential + randomGenerator.Next(mutationLowerBound, mutationUpperBound));

        potential = (byte)(potential is < 30 or > 95 ? 50 : potential);

        return potential;
    }
}
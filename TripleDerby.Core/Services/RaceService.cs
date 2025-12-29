using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Racing;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Core.Services;

public class RaceService(
    ITripleDerbyRepository repository,
    IRandomGenerator randomGenerator,
    ISpeedModifierCalculator speedModifierCalculator,
    IStaminaCalculator staminaCalculator,
    IRaceCommentaryGenerator commentaryGenerator,
    IPurseCalculator purseCalculator,
    IOvertakingManager overtakingManager,
    IEventDetector eventDetector,
    [FromKeyedServices("servicebus")] IMessagePublisher messagePublisher,
    ITimeManager timeManager,
    ILogger<RaceService> logger) : IRaceService
{
    public Task<RaceResult> Get(byte id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<RacesResult>> GetAll(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<RaceRunResult> Race(byte raceId, Guid horseId, CancellationToken cancellationToken)
    {
        var race = await repository.FirstOrDefaultAsync(new RaceSpecification(raceId), cancellationToken);
        var myHorse = await repository.FirstOrDefaultAsync(new HorseForRaceSpecification(horseId), cancellationToken);

        if (race == null)
        {
            throw new ArgumentException($"Race not found with {raceId}");
        }

        if (myHorse == null)
        {
            throw new ArgumentException($"Horse not found with {horseId}");
        }

        // Fetch CPU horses with similar race experience (owned by Racers)
        var fieldSize = randomGenerator.Next(race.MinFieldSize, race.MaxFieldSize + 1);
        var cpuHorseSpec = new SimilarRaceStartsSpecification(
            targetRaceStarts: myHorse.RaceStarts,
            tolerance: 8,
            limit: fieldSize - 1); // -1 for player's horse
        var cpuHorses = await repository.ListAsync(cpuHorseSpec, cancellationToken);

        // Combine player's horse with CPU horses
        var horses = new HashSet<Horse> { myHorse };
        horses.UnionWith(cpuHorses);

        var raceRun = new RaceRun
        {
            RaceId = raceId,
            Race = race,
            Purse = race.Purse,
            ConditionId = GenerateRandomConditionId(),
            Horses = new List<RaceRunHorse>(),
            RaceRunTicks = new List<RaceRunTick>()
        };
        InitializeHorses(raceRun, horses);

        // Calculate total ticks for the race
        var totalTicks = CalculateTotalTicks(race.Furlongs);

        var allHorsesFinished = false;
        short tick = 0;

        // Track previous state for event detection
        var previousPositions = new Dictionary<Guid, int>();
        var previousLanes = new Dictionary<Guid, byte>();
        Guid? previousLeader = null;
        var recentPositionChanges = new Dictionary<Guid, short>(); // Track last tick each horse had a position change
        var recentLaneChanges = new Dictionary<Guid, short>(); // Track last tick each horse had a lane change

        // Run the simulation until all horses finish
        while (!allHorsesFinished)
        {
            tick++;

            // Update each horse's position FIRST
            foreach (var horse in raceRun.Horses)
            {
                // Only update horses that haven't finished
                if (horse.Distance < race.Furlongs)
                {
                    var previousDistance = horse.Distance;
                    UpdateHorsePosition(horse, tick, totalTicks, raceRun);

                    // Check if horse just crossed the finish line
                    if (previousDistance < race.Furlongs && horse.Distance >= race.Furlongs)
                    {
                        var distanceToGo = race.Furlongs - previousDistance;
                        var distanceCovered = horse.Distance - previousDistance;

                        if (distanceCovered > 0)
                        {
                            var fractionOfTick = (double)(distanceToGo / distanceCovered);

                            horse.Time = tick - 1 + fractionOfTick;
                        }
                        else
                        {
                            // Fallback if somehow distance didn't change
                            horse.Time = tick;
                        }

                        // Assign place based on finish order
                        var finishedCount = raceRun.Horses.Count(h => h.Distance >= race.Furlongs);
                        horse.Place = (byte)finishedCount;

                        // NOW cap the distance at finish line
                        horse.Distance = race.Furlongs;
                    }

                    // Handle overtaking and lane changes
                    overtakingManager.HandleOvertaking(horse, raceRun, tick, totalTicks);
                }
                //ApplyRandomEvents(horse, tick);
            }

            // Detect events and generate commentary
            var events = eventDetector.DetectEvents(tick, totalTicks, raceRun, previousPositions, previousLanes, previousLeader, recentPositionChanges, recentLaneChanges);
            var commentary = commentaryGenerator.GenerateCommentary(events, tick, raceRun);

            // THEN create the tick record with the updated positions
            var raceRunTick = new RaceRunTick
            {
                Tick = tick,
                RaceRunTickHorses = new List<RaceRunTickHorse>(),
                Note = commentary
            };

            foreach (var raceRunHorse in raceRun.Horses)
            {
                var raceRunTickHorse = new RaceRunTickHorse
                {
                    Horse = raceRunHorse.Horse,
                    HorseId = raceRunHorse.Horse.Id,
                    Lane = raceRunHorse.Lane,
                    Distance = raceRunHorse.Distance,
                    RaceRunTick = raceRunTick
                };
                raceRunTick.RaceRunTickHorses.Add(raceRunTickHorse);
            }

            raceRun.RaceRunTicks.Add(raceRunTick);

            // Update previous state for next tick
            eventDetector.UpdatePreviousState(raceRun, previousPositions, previousLanes, ref previousLeader);

            // Check if all horses have finished
            allHorsesFinished = raceRun.Horses.All(horse => horse.Distance >= race.Furlongs);

            // Stop if we've reached a high number of ticks
            if (tick > totalTicks * 2)
            {
                break;
            }
        }

        // Determine winners and rewards
        DetermineRaceResults(raceRun);

        // Calculate purse and distribute earnings
        var totalPurse = purseCalculator.CalculateTotalPurse(race.RaceClassId, race.Furlongs);
        var payouts = purseCalculator.CalculateAllPayouts(race.RaceClassId, totalPurse);

        // Update horse earnings for money winners
        foreach (var raceRunHorse in raceRun.Horses.Where(h => payouts.ContainsKey(h.Place)))
        {
            var payout = payouts[raceRunHorse.Place];
            raceRunHorse.Horse.Earnings += payout;
            raceRunHorse.Payout = payout;
        }

        await repository.CreateAsync(raceRun, cancellationToken);

        return new RaceRunResult
        {
            RaceRunId = raceRun.Id,
            RaceId = raceRun.RaceId,
            RaceName = race.Name,
            ConditionId = raceRun.ConditionId,
            ConditionName = raceRun.ConditionId.ToString(),
            TrackId = race.TrackId,
            TrackName = race.Track.Name,
            Furlongs = race.Furlongs,
            SurfaceId = race.SurfaceId,
            SurfaceName = race.Surface.Name,
            PlayByPlay = raceRun.RaceRunTicks
                .Where(t => !string.IsNullOrEmpty(t.Note))
                .Select(t => t.Note)
                .ToList(),
            HorseResults = raceRun.Horses
                .OrderBy(h => h.Place)
                .Select(h => new RaceRunHorseResult
                {
                    HorseId = h.Horse.Id,
                    HorseName = h.Horse.Name,
                    Place = h.Place,
                    Payout = payouts.GetValueOrDefault(h.Place, 0),
                    Time = h.Time
                })
                .ToList()
        };
    }

    public async Task<RaceRequestStatusResult> QueueRaceAsync(byte raceId, Guid horseId, Guid ownerId, CancellationToken cancellationToken = default)
    {
        // Generate correlation ID for this request
        var correlationId = Guid.NewGuid();

        // Create RaceRequest entity for tracking
        var raceRequest = new RaceRequest
        {
            Id = correlationId,
            RaceId = raceId,
            HorseId = horseId,
            OwnerId = ownerId,
            Status = RaceRequestStatus.Pending,
            CreatedDate = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };

        await repository.CreateAsync(raceRequest, cancellationToken);

        // Publish message to Service Bus
        var message = new RaceRequested
        {
            CorrelationId = correlationId,
            RaceId = raceId,
            HorseId = horseId,
            RequestedBy = ownerId,
            RequestedAt = DateTime.UtcNow
        };

        await messagePublisher.PublishAsync(
            message,
            new MessagePublishOptions { Destination = "race-requests" },
            cancellationToken);

        return new RaceRequestStatusResult(
            Id: correlationId,
            RaceId: raceId,
            HorseId: horseId,
            Status: RaceRequestStatus.Pending,
            RaceRunId: null,
            OwnerId: ownerId,
            CreatedDate: raceRequest.CreatedDate,
            ProcessedDate: null,
            UpdatedDate: null,
            FailureReason: null
        );
    }

    public async Task<RaceRequestStatusResult?> GetRequestStatusAsync(byte raceId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await repository.FindAsync<RaceRequest>(requestId, cancellationToken);

        if (request == null || request.RaceId != raceId)
            return null;

        return new RaceRequestStatusResult(
            Id: request.Id,
            RaceId: request.RaceId,
            HorseId: request.HorseId,
            Status: request.Status,
            RaceRunId: request.RaceRunId,
            OwnerId: request.OwnerId,
            CreatedDate: request.CreatedDate,
            ProcessedDate: request.ProcessedDate,
            UpdatedDate: request.UpdatedDate,
            FailureReason: request.FailureReason
        );
    }

    public async Task<bool> ReplayRaceRequest(Guid raceRequestId, CancellationToken cancellationToken = default)
    {
        if (raceRequestId == Guid.Empty)
            throw new ArgumentException("Invalid id", nameof(raceRequestId));

        var entity = await repository.FindAsync<RaceRequest>(raceRequestId, cancellationToken);

        if (entity is null)
            return false;

        // If the request already completed, don't replay
        if (entity.Status == RaceRequestStatus.Completed)
        {
            logger.LogInformation("Not replaying RaceRequestId={Id} because it is already Completed", entity.Id);
            return false;
        }

        // If previously failed, reset to Pending so processors will pick it up.
        var originalStatus = entity.Status;
        var originalFailureReason = entity.FailureReason;
        try
        {
            if (entity.Status == RaceRequestStatus.Failed)
            {
                entity.Status = RaceRequestStatus.Pending;
                entity.FailureReason = null;
                entity.ProcessedDate = null;
                entity.UpdatedDate = timeManager.OffsetUtcNow();

                await repository.UpdateAsync(entity, cancellationToken);

                logger.LogInformation("Marked RaceRequestId={Id} as Pending for replay", entity.Id);
            }

            var msg = new RaceRequested
            {
                CorrelationId = entity.Id,
                RaceId = entity.RaceId,
                HorseId = entity.HorseId,
                RequestedBy = entity.OwnerId,
                RequestedAt = entity.CreatedDate.DateTime
            };

            await messagePublisher.PublishAsync(
                msg,
                new MessagePublishOptions { Destination = "race-requests" },
                cancellationToken);

            logger.LogInformation("Replayed RaceRequested event for RaceRequestId={Id}", entity.Id);

            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Replay publishing cancelled for RaceRequestId={Id}", entity.Id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to replay RaceRequested event for RaceRequestId={Id}", entity.Id);

            // Attempt to restore Failed status and record failure reason so it can be retried later
            try
            {
                var rr = await repository.FindAsync<RaceRequest>(raceRequestId, cancellationToken);
                if (rr != null)
                {
                    rr.Status = RaceRequestStatus.Failed;
                    rr.FailureReason = $"Replay publish failed: {ex.Message}";
                    rr.ProcessedDate = timeManager.OffsetUtcNow();
                    rr.UpdatedDate = timeManager.OffsetUtcNow();
                    await repository.UpdateAsync(rr, cancellationToken);
                }
            }
            catch (Exception updEx)
            {
                logger.LogWarning(updEx, "Failed to persist replay-publish-failure metadata for RaceRequestId={Id}", entity.Id);
            }

            // Restore original status in-memory (no DB change) for calling code if needed
            entity.Status = originalStatus;
            entity.FailureReason = originalFailureReason;

            throw;
        }
    }

    public async Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default)
    {
        if (maxDegreeOfParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

        cancellationToken.ThrowIfCancellationRequested();

        // Fetch all RaceRequests that are not Completed
        var requests = await repository.ListAsync<RaceRequest>(rr => rr.Status != RaceRequestStatus.Completed, cancellationToken);

        if (requests == null || requests.Count == 0)
        {
            logger.LogInformation("No non-complete race requests found to replay.");
            return 0;
        }

        logger.LogInformation("Replaying {Count} non-complete race requests (maxConcurrency={Max})", requests.Count, maxDegreeOfParallelism);

        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
        var tasks = new List<Task>();
        var publishedCount = 0;

        foreach (var r in requests)
        {
            await semaphore.WaitAsync(cancellationToken);
            var task = Task.Run(async () =>
            {
                try
                {
                    // If previously failed, mark Pending before publishing so processors will pick it up
                    if (r.Status == RaceRequestStatus.Failed)
                    {
                        try
                        {
                            r.Status = RaceRequestStatus.Pending;
                            r.FailureReason = null;
                            r.ProcessedDate = null;
                            r.UpdatedDate = timeManager.OffsetUtcNow();
                            await repository.UpdateAsync(r, cancellationToken);
                        }
                        catch (Exception updateEx)
                        {
                            logger.LogWarning(updateEx, "Failed to mark RaceRequestId={Id} Pending before replay; skipping", r.Id);
                            return;
                        }
                    }

                    var msg = new RaceRequested
                    {
                        CorrelationId = r.Id,
                        RaceId = r.RaceId,
                        HorseId = r.HorseId,
                        RequestedBy = r.OwnerId,
                        RequestedAt = r.CreatedDate.DateTime
                    };

                    await messagePublisher.PublishAsync(
                        msg,
                        new MessagePublishOptions { Destination = "race-requests" },
                        cancellationToken);

                    Interlocked.Increment(ref publishedCount);
                    logger.LogInformation("Replayed RaceRequested for RaceRequestId={Id}", r.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to replay RaceRequested for RaceRequestId={Id}", r.Id);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        logger.LogInformation("ReplayAllNonComplete finished. Published {Published} of {Total}", publishedCount, requests.Count);

        return publishedCount;
    }

    private void InitializeHorses(RaceRun raceRun, IEnumerable<Horse> horses)
    {
        var horseList = horses.ToList();
        var fieldSize = horseList.Count;

        // Random lane assignment for fairness - creates shuffled lane numbers: [1, 2, 3, ..., fieldSize] in random order
        var shuffledLanes = Enumerable.Range(1, fieldSize)
            .OrderBy(_ => randomGenerator.Next())
            .ToArray();

        for (int i = 0; i < horseList.Count; i++)
        {
            var raceRunHorse = new RaceRunHorse
            {
                Horse = horseList[i],
                InitialStamina = horseList[i].Stamina,
                CurrentStamina = horseList[i].Stamina,
                Lane = (byte)shuffledLanes[i],          // Random lane assignment
                TicksSinceLastLaneChange = 10           // Start with full cooldown elapsed
            };
            raceRun.Horses.Add(raceRunHorse);
        }
    }

    private void UpdateHorsePosition(RaceRunHorse raceRunHorse, short tick, short totalTicks, RaceRun raceRun)
    {
        var baseSpeed = RaceModifierConfig.AverageBaseSpeed;
        var raceProgress = (double)tick / totalTicks;

        var context = new ModifierContext(
            CurrentTick: tick,
            TotalTicks: totalTicks,
            Horse: raceRunHorse.Horse,
            RaceCondition: raceRun.ConditionId,
            RaceSurface: raceRun.Race.SurfaceId,
            RaceFurlongs: raceRun.Race.Furlongs
        );

        // Modifier Pipeline: Stats → Environment → Phase → Stamina → Random
        // Apply stat modifiers (Speed + Agility)
        var statModifier = speedModifierCalculator.CalculateStatModifiers(context);
        baseSpeed *= statModifier;

        // Apply environmental modifiers (Surface + Condition)
        var envModifier = speedModifierCalculator.CalculateEnvironmentalModifiers(context);
        baseSpeed *= envModifier;

        // Apply phase modifiers (LegType timing or conditional bonuses)
        var phaseModifier = speedModifierCalculator.CalculatePhaseModifiers(context, raceRun);
        baseSpeed *= phaseModifier;

        // Apply stamina modifier (speed penalty when stamina low)
        var staminaModifier = speedModifierCalculator.CalculateStaminaModifier(raceRunHorse);
        baseSpeed *= staminaModifier;

        // Apply risky lane change penalty (if active)
        if (raceRunHorse.SpeedPenaltyTicksRemaining > 0)
        {
            baseSpeed *= RaceModifierConfig.RiskyLaneChangeSpeedPenalty;
            raceRunHorse.SpeedPenaltyTicksRemaining--;
        }

        // Apply traffic response effects (speed capping / frustration)
        overtakingManager.ApplyTrafficEffects(raceRunHorse, raceRun, ref baseSpeed);

        // Apply random variance (±1% per tick)
        var randomVariance = speedModifierCalculator.ApplyRandomVariance();
        baseSpeed *= randomVariance;

        // Calculate current speed after all modifiers
        var currentSpeed = baseSpeed;

        // Safety check: ensure speed is valid (not NaN, Infinity, or negative)
        if (double.IsNaN(currentSpeed) || double.IsInfinity(currentSpeed))
        {
            currentSpeed = 0.001; // Fallback for numerical errors
        }
        else if (currentSpeed < 0)
        {
            currentSpeed = 0; // Horse doesn't move backwards
        }
        // Note: No upper clamp needed - extreme fast speeds are handled by natural physics

        // Update horse position
        raceRunHorse.Distance += (decimal)currentSpeed;

        // Deplete stamina based on effort
        var depletionAmount = staminaCalculator.CalculateDepletionAmount(
            raceRunHorse.Horse,
            raceRun.Race.Furlongs,
            currentSpeed,
            RaceModifierConfig.AverageBaseSpeed,
            raceProgress);

        raceRunHorse.CurrentStamina = Math.Max(0, raceRunHorse.CurrentStamina - depletionAmount);
    }

    private static void DetermineRaceResults(RaceRun raceRun)
    {
        // Sort horses by time and assign places
        var sortedHorses = raceRun.Horses.OrderBy(h => h.Time).ToList();
        byte place = 1;
        
        foreach (var horse in sortedHorses)
        {
            horse.Place = place;
            horse.Horse.RaceStarts++;

            // Assign race result IDs and increment win/place/show counters
            switch (place)
            {
                case 1:
                    raceRun.WinHorseId = horse.Horse.Id;
                    horse.Horse.RaceWins++;
                    break;
                case 2:
                    raceRun.PlaceHorseId = horse.Horse.Id;
                    horse.Horse.RacePlace++;
                    break;
                case 3:
                    raceRun.ShowHorseId = horse.Horse.Id;
                    horse.Horse.RaceShow++;
                    break;
            }

            place++;
        }
    }

    private static short CalculateTotalTicks(decimal furlongs)
    {
        // At 0.0422 furlongs/tick (derived from ~38 mph), calculate required ticks
        // This ensures horses can actually complete the race distance
        return (short)Math.Ceiling((double)furlongs / RaceModifierConfig.AverageBaseSpeed);
    }

    private ConditionId GenerateRandomConditionId()
    {
        var values = Enum.GetValues(typeof(ConditionId));
        return (ConditionId)values.GetValue(randomGenerator.Next(values.Length))!;
    }
}

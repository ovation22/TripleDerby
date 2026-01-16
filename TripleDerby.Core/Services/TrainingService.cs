using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Services;

/// <summary>
/// Core training service for request orchestration.
/// </summary>
public class TrainingService(
    ITripleDerbyRepository repository,
    IMessagePublisher messagePublisher,
    ITimeManager timeManager,
    ILogger<TrainingService> logger) : ITrainingService
{
    public async Task<TrainingResult> Get(byte id)
    {
        var training = await repository.SingleOrDefaultAsync(new TrainingSpecification(id));

        if (training is null)
        {
            throw new KeyNotFoundException($"Training with ID '{id}' was not found.");
        }

        return new TrainingResult
        {
            Id = training.Id,
            Name = training.Name,
            Description = training.Description
        };
    }

    public async Task<IEnumerable<TrainingsResult>> GetAll()
    {
        var trainings = await repository.GetAllAsync<Training>();

        return trainings.Select(x => new TrainingsResult
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description
        });
    }

    /// <summary>
    /// Queues a training session for async processing.
    /// </summary>
    public async Task<TrainingRequested> QueueTrainingAsync(
        Guid horseId,
        byte trainingId,
        Guid sessionId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Queueing training request: Horse={HorseId}, Training={TrainingId}, Session={SessionId}",
            horseId, trainingId, sessionId);

        var existingRequest = await repository.FindAsync<TrainingRequest>(sessionId, cancellationToken);
        if (existingRequest != null)
        {
            logger.LogInformation("Training request {SessionId} already exists with status {Status}, returning existing",
                sessionId, existingRequest.Status);

            return new TrainingRequested(
                existingRequest.Id,
                existingRequest.HorseId,
                existingRequest.TrainingId,
                existingRequest.SessionId,
                existingRequest.OwnerId,
                existingRequest.CreatedDate
            );
        }

        var horse = await repository.FindAsync<Horse>(horseId, cancellationToken);
        if (horse == null)
            throw new KeyNotFoundException($"Horse with ID {horseId} not found");

        var training = await repository.FindAsync<Training>(trainingId, cancellationToken);
        if (training == null)
            throw new KeyNotFoundException($"Training with ID {trainingId} not found");

        var trainingRequest = new TrainingRequest
        {
            Id = sessionId,
            HorseId = horseId,
            TrainingId = trainingId,
            SessionId = sessionId,
            OwnerId = ownerId,
            Status = TrainingRequestStatus.Pending,
            CreatedDate = timeManager.OffsetUtcNow(),
            CreatedBy = ownerId
        };

        trainingRequest = await repository.CreateAsync(trainingRequest, cancellationToken);

        var message = new TrainingRequested(
            trainingRequest.Id,
            trainingRequest.HorseId,
            trainingRequest.TrainingId,
            trainingRequest.SessionId,
            trainingRequest.OwnerId,
            trainingRequest.CreatedDate
        );

        await messagePublisher.PublishAsync(message, cancellationToken: cancellationToken);

        logger.LogInformation("Training request queued successfully: RequestId={RequestId}", trainingRequest.Id);

        return message;
    }

    /// <summary>
    /// Gets the status of a training request.
    /// </summary>
    public async Task<TrainingRequestStatusResult?> GetRequestStatus(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var request = await repository.FindAsync<TrainingRequest>(sessionId, cancellationToken);

        if (request == null)
            return null;

        return new TrainingRequestStatusResult
        {
            Id = request.Id,
            HorseId = request.HorseId,
            TrainingId = request.TrainingId,
            SessionId = request.SessionId,
            Status = request.Status,
            FailureReason = request.FailureReason,
            CreatedDate = request.CreatedDate,
            ProcessedDate = request.ProcessedDate
        };
    }

    /// <summary>
    /// Re-publishes a failed training request.
    /// </summary>
    public async Task ReplayTrainingRequest(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var request = await repository.FindAsync<TrainingRequest>(sessionId, cancellationToken);

        if (request == null)
            throw new KeyNotFoundException($"Training request {sessionId} not found");

        if (request.Status != TrainingRequestStatus.Failed)
            throw new InvalidOperationException($"Training request {sessionId} is not in Failed status");

        var message = new TrainingRequested(
            request.Id,
            request.HorseId,
            request.TrainingId,
            request.SessionId,
            request.OwnerId,
            request.CreatedDate
        );

        await messagePublisher.PublishAsync(message, cancellationToken: cancellationToken);

        logger.LogInformation("Replayed training request: RequestId={RequestId}", request.Id);
    }

    /// <summary>
    /// Gets available training options for a horse.
    /// </summary>
    public async Task<List<TrainingOptionResult>> GetTrainingOptions(Guid horseId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var horse = await repository.FindAsync<Horse>(horseId, cancellationToken);
        if (horse == null)
            throw new KeyNotFoundException($"Horse with ID {horseId} not found");

        var allTrainings = await repository.GetAllAsync<Training>(cancellationToken);

        return allTrainings.Select(t => new TrainingOptionResult
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            SpeedModifier = t.SpeedModifier,
            StaminaModifier = t.StaminaModifier,
            AgilityModifier = t.AgilityModifier,
            DurabilityModifier = t.DurabilityModifier,
            HappinessCost = t.HappinessCost,
            OverworkRisk = t.OverworkRisk,
            IsRecovery = t.IsRecovery
        }).ToList();
    }

    /// <summary>
    /// Gets training history for a horse.
    /// </summary>
    public async Task<PagedList<TrainingHistoryResult>> GetTrainingHistory(Guid horseId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        // Verify horse exists
        var horse = await repository.FindAsync<Horse>(horseId, cancellationToken);
        if (horse == null)
        {
            throw new KeyNotFoundException($"Horse with ID '{horseId}' was not found.");
        }

        // Specification handles filtering, sorting, pagination, and projection
        var spec = new TrainingSessionHistorySpecification(horseId, request);
        return await repository.ListAsync(spec, cancellationToken);
    }
}

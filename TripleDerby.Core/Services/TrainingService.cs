using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;

namespace TripleDerby.Core.Services;

public class TrainingService(ITripleDerbyRepository repository) : ITrainingService
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

    public async Task<TrainingSessionResult> Train(byte trainingId, Guid horseId)
    {
        const string result = "Hooray!";
        var trainingSession = new TrainingSession
        {
            TrainingId = trainingId,
            HorseId = horseId,
            Result = result
        };

        await repository.CreateAsync(trainingSession);

        return new TrainingSessionResult { Result = result };
    }
}

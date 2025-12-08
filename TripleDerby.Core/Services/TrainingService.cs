using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;

namespace TripleDerby.Core.Services;

public class TrainingService : ITrainingService
{
    private readonly ITripleDerbyRepository _repository;

    public TrainingService(
        ITripleDerbyRepository repository
    )
    {
        _repository = repository;
    }

    public async Task<TrainingResult> Get(byte id)
    {
        var training = await _repository.SingleOrDefaultAsync(new TrainingSpecification(id));

        return new TrainingResult
        {
            Id = training.Id,
            Name = training.Name,
            Description = training.Description
        };
    }

    public async Task<IEnumerable<TrainingsResult>> GetAll()
    {
        var trainings = await _repository.GetAllAsync<Training>();

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

        await _repository.CreateAsync(trainingSession);

        return new TrainingSessionResult { Result = result };
    }
}

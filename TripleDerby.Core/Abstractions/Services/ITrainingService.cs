using TripleDerby.SharedKernel;

namespace TripleDerby.Core.Abstractions.Services;

public interface ITrainingService
{
    Task<TrainingResult> Get(byte id);
    Task<IEnumerable<TrainingsResult>> GetAll();
    Task<TrainingSessionResult> Train(byte trainingId, Guid horseId);
}

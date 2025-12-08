using TripleDerby.SharedKernel;

namespace TripleDerby.Core.Abstractions.Services;

public interface IFeedingService
{
    Task<FeedingResult> Get(byte id);
    Task<IEnumerable<FeedingsResult>> GetAll();
    Task<FeedingSessionResult> Feed(byte feedingId, Guid horseId);
}

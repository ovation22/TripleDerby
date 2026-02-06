using TripleDerby.SharedKernel;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IMessagesApiClient
{
    Task<MessageRequestsSummaryResult?> GetSummaryAsync(
        CancellationToken cancellationToken = default);
}

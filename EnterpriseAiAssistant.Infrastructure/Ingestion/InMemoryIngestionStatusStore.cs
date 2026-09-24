using System.Collections.Concurrent;
using EnterpriseAiAssistant.Application.Ingestion;
using EnterpriseAiAssistant.Domain.Ingestion;

namespace EnterpriseAiAssistant.Infrastructure.Ingestion;

/// <summary>
/// Tracks ingestion run status in an in-memory, process-wide
/// dictionary. Sufficient for the prototype's single-instance
/// deployment; swap for a small SQL/Cosmos-backed store (behind the
/// same IIngestionStatusStore port) if ingestion status needs to
/// survive restarts or be shared across instances.
/// </summary>
public sealed class InMemoryIngestionStatusStore : IIngestionStatusStore
{
    private readonly ConcurrentDictionary<Guid, IngestionRun> _runs = new();

    public Task<IngestionRun> StartAsync(
        IngestionSourceType sourceType,
        string target,
        CancellationToken cancellationToken = default)
    {
        var run = IngestionRun.Start(sourceType, target);

        _runs[run.Id] = run;

        return Task.FromResult(run);
    }

    public Task MarkSucceededAsync(
        Guid ingestionRunId,
        CancellationToken cancellationToken = default)
    {
        if (_runs.TryGetValue(ingestionRunId, out var run))
        {
            run.MarkSucceeded();
        }

        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(
        Guid ingestionRunId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        if (_runs.TryGetValue(ingestionRunId, out var run))
        {
            run.MarkFailed(errorMessage);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IngestionRun>> GetRecentAsync(
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IngestionRun> recent = _runs.Values
            .OrderByDescending(r => r.StartedAt)
            .Take(count)
            .ToList();

        return Task.FromResult(recent);
    }
}

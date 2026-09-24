using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseAiAssistant.Ingestion;

/// <summary>
/// Drains <see cref="IngestionQueue"/> continuously in the background.
/// Each work item is processed independently (failure isolation: one
/// bad job or corrupt PDF never stops the worker or blocks the next
/// item), retried with backoff up to a fixed limit, and its outcome is
/// always recorded via IIngestionStatusStore whether it ultimately
/// succeeds or fails.
/// </summary>
public sealed class BackgroundIngestionWorker : BackgroundService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly IngestionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundIngestionWorker> _logger;

    public BackgroundIngestionWorker(
        IngestionQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundIngestionWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.ReadAllAsync(stoppingToken))
        {
            // Isolated per item: an exception here is fully handled
            // inside ProcessWithRetryAsync, so it can never terminate
            // the loop or affect any other queued item.
            await ProcessWithRetryAsync(item, stoppingToken);
        }
    }

    private async Task ProcessWithRetryAsync(IngestionWorkItem item, CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                switch (item.Kind)
                {
                    case IngestionWorkItemKind.JobSync:
                        var jobProcessor = scope.ServiceProvider.GetRequiredService<JobDataIngestionProcessor>();
                        await jobProcessor.ProcessAsync(item.JobNumber!, stoppingToken);
                        break;

                    case IngestionWorkItemKind.DocumentSync:
                        var documentProcessor = scope.ServiceProvider.GetRequiredService<DocumentIngestionProcessor>();
                        await documentProcessor.ProcessAsync(
                            item.DocumentFileName!,
                            item.DocumentContentType!,
                            item.DocumentContent!,
                            stoppingToken);
                        break;
                }

                return; // succeeded
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Ingestion attempt {Attempt}/{MaxAttempts} failed for {Kind} '{Target}'. Retrying in {Delay}.",
                    attempt, MaxAttempts, item.Kind, item.JobNumber ?? item.DocumentFileName, RetryDelay);

                await Task.Delay(RetryDelay, stoppingToken);
            }
            catch (Exception ex)
            {
                // Final attempt failed: the processor has already
                // recorded this as Failed in IIngestionStatusStore.
                // Swallow here so one permanently-failing item can
                // never take down the background worker.
                _logger.LogError(
                    ex,
                    "Ingestion permanently failed after {MaxAttempts} attempts for {Kind} '{Target}'.",
                    MaxAttempts, item.Kind, item.JobNumber ?? item.DocumentFileName);
            }
        }
    }
}

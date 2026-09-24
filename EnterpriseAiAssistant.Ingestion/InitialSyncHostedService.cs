using EnterpriseAiAssistant.Application.Ingestion;
using EnterpriseAiAssistant.Infrastructure.Graph;
using EnterpriseAiAssistant.Infrastructure.Jobs;
using EnterpriseAiAssistant.Infrastructure.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseAiAssistant.Ingestion;

/// <summary>
/// One-shot startup task: seeds the prototype JOB SQL database, ensures
/// the Azure AI Search index and Cosmos DB container exist, and enqueues
/// every job so the full ingestion → embedding → retrieval → Semantic
/// Kernel → answer flow works immediately after the app starts, with no
/// manual trigger required. Runs in the background so it never delays
/// the app becoming ready to serve chat requests.
/// </summary>
public sealed class InitialSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IngestionQueue _queue;
    private readonly ILogger<InitialSyncHostedService> _logger;

    public InitialSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IngestionQueue queue,
        ILogger<InitialSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;

            var jobDbContext = services.GetRequiredService<JobDbContext>();
            await jobDbContext.Database.EnsureCreatedAsync(stoppingToken);
            await JobDbSeeder.SeedAsync(jobDbContext, stoppingToken);

            await services.GetRequiredService<JobKnowledgeIndexInitializer>()
                .EnsureIndexExistsAsync(stoppingToken);

            await services.GetRequiredService<CosmosClientFactory>()
                .GetContainerAsync(stoppingToken);

            var reader = services.GetRequiredService<IJobDataSourceReader>();
            var jobs = await reader.GetAllJobsAsync(stoppingToken);

            foreach (var job in jobs)
            {
                _queue.Enqueue(IngestionWorkItem.ForJob(job.JobNumber));
            }

            _logger.LogInformation(
                "Initial sync enqueued {Count} job(s) for ingestion.", jobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initial ingestion sync failed to complete.");
        }
    }
}

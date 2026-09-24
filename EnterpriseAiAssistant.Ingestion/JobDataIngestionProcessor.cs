using EnterpriseAiAssistant.Application.Ingestion;
using EnterpriseAiAssistant.Domain.Ingestion;
using Microsoft.Extensions.Logging;

namespace EnterpriseAiAssistant.Ingestion;

/// <summary>
/// Reads one Job from the SQL Server source of truth and writes both
/// derived representations from it: the Cosmos DB relationship graph
/// and the Azure AI Search job-evidence index. Both writes happen from
/// the same JobRecord so the two projections can never drift from each
/// other, and they run concurrently since they are independent stores.
/// </summary>
public sealed class JobDataIngestionProcessor
{
    private readonly IJobDataSourceReader _jobDataSourceReader;
    private readonly IJobGraphIndexWriter _jobGraphIndexWriter;
    private readonly IJobEvidenceIndexWriter _jobEvidenceIndexWriter;
    private readonly IIngestionStatusStore _statusStore;
    private readonly ILogger<JobDataIngestionProcessor> _logger;

    public JobDataIngestionProcessor(
        IJobDataSourceReader jobDataSourceReader,
        IJobGraphIndexWriter jobGraphIndexWriter,
        IJobEvidenceIndexWriter jobEvidenceIndexWriter,
        IIngestionStatusStore statusStore,
        ILogger<JobDataIngestionProcessor> logger)
    {
        _jobDataSourceReader = jobDataSourceReader;
        _jobGraphIndexWriter = jobGraphIndexWriter;
        _jobEvidenceIndexWriter = jobEvidenceIndexWriter;
        _statusStore = statusStore;
        _logger = logger;
    }

    public async Task ProcessAsync(string jobNumber, CancellationToken cancellationToken)
    {
        var run = await _statusStore.StartAsync(IngestionSourceType.JobSqlData, jobNumber, cancellationToken);

        try
        {
            var job = await _jobDataSourceReader.GetJobAsync(jobNumber, cancellationToken);

            if (job is null)
            {
                throw new InvalidOperationException($"Job '{jobNumber}' was not found in the source-of-truth database.");
            }

            await Task.WhenAll(
                _jobGraphIndexWriter.SyncJobGraphAsync(job, cancellationToken),
                _jobEvidenceIndexWriter.IndexJobEvidenceAsync(job, cancellationToken));

            await _statusStore.MarkSucceededAsync(run.Id, cancellationToken);

            _logger.LogInformation(
                "Ingested job {JobNumber} into Cosmos DB and Azure AI Search.", jobNumber);
        }
        catch (Exception ex)
        {
            await _statusStore.MarkFailedAsync(run.Id, ex.Message, cancellationToken);

            _logger.LogError(ex, "Failed to ingest job {JobNumber}.", jobNumber);

            throw;
        }
    }
}

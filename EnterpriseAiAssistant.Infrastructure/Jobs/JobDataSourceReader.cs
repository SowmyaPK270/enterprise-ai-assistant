using EnterpriseAiAssistant.Application.Ingestion;
using EnterpriseAiAssistant.Domain.Jobs;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAiAssistant.Infrastructure.Jobs;

/// <summary>
/// Reads the JOB SQL Server source-of-truth data and flattens it into
/// <see cref="JobRecord"/> so the ingestion pipeline can build both the
/// Cosmos DB graph representation and the Azure AI Search job-evidence
/// index from the exact same shape.
/// </summary>
public sealed class JobDataSourceReader : IJobDataSourceReader
{
    private readonly JobDbContext _dbContext;

    public JobDataSourceReader(JobDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<JobRecord>> GetAllJobsAsync(
        CancellationToken cancellationToken = default)
    {
        var jobNumbers = await _dbContext.Jobs
            .AsNoTracking()
            .Select(j => j.JobNumber)
            .ToListAsync(cancellationToken);

        var records = new List<JobRecord>(jobNumbers.Count);

        foreach (var jobNumber in jobNumbers)
        {
            var record = await GetJobAsync(jobNumber, cancellationToken);

            if (record is not null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    public async Task<JobRecord?> GetJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Well!.Field)
            .FirstOrDefaultAsync(j => j.JobNumber == jobNumber, cancellationToken);

        if (job is null)
        {
            return null;
        }

        var operations = await _dbContext.Operations
            .AsNoTracking()
            .Where(o => o.JobId == job.Id)
            .OrderBy(o => o.SequenceNumber)
            .ToListAsync(cancellationToken);

        var operationRecords = new List<OperationRecord>(operations.Count);

        foreach (var operation in operations)
        {
            var runs = await _dbContext.Runs
                .AsNoTracking()
                .Where(r => r.OperationId == operation.Id)
                .ToListAsync(cancellationToken);

            var runRecords = new List<RunRecord>(runs.Count);

            foreach (var run in runs)
            {
                var crew = await _dbContext.Personnel
                    .AsNoTracking()
                    .Where(p => p.RunId == run.Id)
                    .Select(p => new PersonnelRecord(p.FullName, p.Role))
                    .ToListAsync(cancellationToken);

                runRecords.Add(new RunRecord(
                    run.Name, run.StartedAt, run.CompletedAt, run.Result, crew));
            }

            operationRecords.Add(new OperationRecord(
                operation.Name, operation.Description, operation.SequenceNumber, runRecords));
        }

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.JobId == job.Id)
            .Select(p => new ProductRecord(p.Name, p.Quantity, p.UnitOfMeasure))
            .ToListAsync(cancellationToken);

        var incidents = await _dbContext.QualityIncidents
            .AsNoTracking()
            .Where(q => q.JobId == job.Id)
            .Select(q => new QualityIncidentRecord(q.Severity, q.Description, q.ReportedAt))
            .ToListAsync(cancellationToken);

        return new JobRecord(
            job.JobNumber,
            job.JobType,
            job.Status.ToString(),
            job.ClientName,
            job.Well?.Name ?? string.Empty,
            job.Well?.Field?.Name ?? string.Empty,
            job.MobilizationDate,
            job.CompletionDate,
            operationRecords,
            products,
            incidents);
    }
}

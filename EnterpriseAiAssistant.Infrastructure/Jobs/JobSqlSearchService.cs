using System.Text;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using EnterpriseAiAssistant.Domain.Jobs;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAiAssistant.Infrastructure.Jobs;

/// <summary>
/// Backs the "MsSqlSearch" Semantic Kernel plugin. Every method is a
/// plain, deterministic relational query against JobDbContext — no
/// embeddings, no semantic similarity. Results are formatted as compact
/// text so they can be handed to the LLM directly as retrieved context.
/// </summary>
public sealed class JobSqlSearchService : IJobSqlSearchService
{
    private readonly JobDbContext _dbContext;

    public JobSqlSearchService(JobDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetJobDetailsAsync(
        string jobNumber,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Well!.Field)
            .FirstOrDefaultAsync(j => j.JobNumber == jobNumber, cancellationToken);

        if (job is null)
        {
            return $"No job was found with job number '{jobNumber}'.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Job {job.JobNumber} ({job.JobType}) — status: {job.Status}.");
        sb.AppendLine($"Client: {job.ClientName}.");
        sb.AppendLine($"Well: {job.Well?.Name} in field {job.Well?.Field?.Name}.");
        sb.AppendLine($"Mobilization date: {job.MobilizationDate:yyyy-MM-dd}.");
        sb.AppendLine(job.CompletionDate is not null
            ? $"Completion date: {job.CompletionDate:yyyy-MM-dd}."
            : "Completion date: not yet completed.");

        return sb.ToString();
    }

    public async Task<string> ListJobsAsync(
       string? status,
       string? clientName,
       string? wellName,
       string? fieldName,
       DateTimeOffset? fromDate,
       DateTimeOffset? toDate,
       CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Well!.Field)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<JobStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(j => j.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(clientName))
        {
            query = query.Where(j => j.ClientName.Contains(clientName));
        }

        if (!string.IsNullOrWhiteSpace(wellName))
        {
            query = query.Where(j => j.Well != null && j.Well.Name.Contains(wellName));
        }

        if (!string.IsNullOrWhiteSpace(fieldName))
        {
            query = query.Where(j => j.Well != null && j.Well.Field != null && j.Well.Field.Name.Contains(fieldName));
        }

        if (fromDate is not null)
        {
            query = query.Where(j => j.MobilizationDate >= fromDate);
        }

        if (toDate is not null)
        {
            query = query.Where(j => j.MobilizationDate <= toDate);
        }

        var jobs = await query
            .OrderByDescending(j => j.MobilizationDate)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return "No jobs matched the given filters.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{jobs.Count} job(s) matched:");

        foreach (var job in jobs)
        {
            sb.AppendLine(
                $"- {job.JobNumber} | {job.JobType} | {job.Status} | client: {job.ClientName} " +
                $"| well: {job.Well?.Name} | field: {job.Well?.Field?.Name} | mobilized: {job.MobilizationDate:yyyy-MM-dd}");
        }

        return sb.ToString();
    }

    public async Task<string> GetOperationCountByFieldAsync(
        IReadOnlyList<string> fieldNames,
        CancellationToken cancellationToken = default)
    {
        // Join Job -> Well -> Field explicitly rather than relying on
        // flattened/denormalized text, so field-level aggregation is
        // always exact even when the graph/search projections don't
        // carry the relationship.
        var jobsQuery = _dbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Well!.Field)
            .Where(j => j.Well != null && j.Well.Field != null);

        if (fieldNames.Count > 0)
        {
            jobsQuery = jobsQuery.Where(j => fieldNames.Contains(j.Well!.Field!.Name));
        }

        var jobs = await jobsQuery.ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return "No jobs were found for the requested field(s).";
        }

        var jobIds = jobs.Select(j => j.Id).ToList();

        var operationCountsByJobId = await _dbContext.Operations
            .AsNoTracking()
            .Where(o => jobIds.Contains(o.JobId))
            .GroupBy(o => o.JobId)
            .Select(g => new { JobId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.JobId, x => x.Count, cancellationToken);

        var byField = jobs
            .GroupBy(j => j.Well!.Field!.Name)
            .Select(g => new
            {
                FieldName = g.Key,
                JobCount = g.Count(),
                OperationCount = g.Sum(j => operationCountsByJobId.GetValueOrDefault(j.Id, 0))
            })
            .OrderByDescending(x => x.OperationCount);

        var sb = new StringBuilder();
        sb.AppendLine("Operation counts by field:");

        foreach (var field in byField)
        {
            sb.AppendLine($"- {field.FieldName}: {field.OperationCount} operation(s) across {field.JobCount} job(s).");
        }

        return sb.ToString();
    }

    public async Task<string> CountJobsByStatusAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Jobs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<JobStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(j => j.Status == parsedStatus);
            var count = await query.CountAsync(cancellationToken);
            return $"{count} job(s) have status '{parsedStatus}'.";
        }

        var total = await query.CountAsync(cancellationToken);
        return $"{total} job(s) in total.";
    }

    public async Task<string> GetOperationsAndRunResultsAsync(
        string jobNumber,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobNumber == jobNumber, cancellationToken);

        if (job is null)
        {
            return $"No job was found with job number '{jobNumber}'.";
        }

        var operations = await _dbContext.Operations
            .AsNoTracking()
            .Where(o => o.JobId == job.Id)
            .OrderBy(o => o.SequenceNumber)
            .ToListAsync(cancellationToken);

        if (operations.Count == 0)
        {
            return $"Job '{jobNumber}' has no recorded operations.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Job {job.JobNumber} — operations, runs, and results:");

        foreach (var operation in operations)
        {
            var runs = await _dbContext.Runs
                .AsNoTracking()
                .Where(r => r.OperationId == operation.Id)
                .ToListAsync(cancellationToken);

            sb.AppendLine($"- {operation.Name} (step {operation.SequenceNumber}): {operation.Description}");

            if (runs.Count == 0)
            {
                sb.AppendLine("    - no runs recorded.");
                continue;
            }

            foreach (var run in runs)
            {
                sb.AppendLine(
                    $"    - {run.Name} -> {run.Result} " +
                    $"(started {run.StartedAt:yyyy-MM-dd HH:mm}, " +
                    $"completed {(run.CompletedAt is not null ? run.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm") : "not completed")})");
            }
        }

        return sb.ToString();
    }

    public async Task<string> GetPersonnelForJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobNumber == jobNumber, cancellationToken);

        if (job is null)
        {
            return $"No job was found with job number '{jobNumber}'.";
        }

        var operationIds = await _dbContext.Operations
            .AsNoTracking()
            .Where(o => o.JobId == job.Id)
            .Select(o => new { o.Id, o.Name, o.SequenceNumber })
            .OrderBy(o => o.SequenceNumber)
            .ToListAsync(cancellationToken);

        if (operationIds.Count == 0)
        {
            return $"Job '{jobNumber}' has no recorded operations.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Job {job.JobNumber} — personnel by operation/run:");

        foreach (var operation in operationIds)
        {
            var runs = await _dbContext.Runs
                .AsNoTracking()
                .Where(r => r.OperationId == operation.Id)
                .ToListAsync(cancellationToken);

            foreach (var run in runs)
            {
                var crew = await _dbContext.Personnel
                    .AsNoTracking()
                    .Where(p => p.RunId == run.Id)
                    .ToListAsync(cancellationToken);

                var crewText = crew.Count == 0
                    ? "no personnel recorded"
                    : string.Join(", ", crew.Select(p => $"{p.FullName} ({p.Role})"));

                sb.AppendLine($"- {operation.Name} / {run.Name}: {crewText}");
            }
        }

        return sb.ToString();
    }

    public async Task<string> GetQualityIncidentsForJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobNumber == jobNumber, cancellationToken);

        if (job is null)
        {
            return $"No job was found with job number '{jobNumber}'.";
        }

        var incidents = await _dbContext.QualityIncidents
            .AsNoTracking()
            .Where(q => q.JobId == job.Id)
            .OrderBy(q => q.ReportedAt)
            .ToListAsync(cancellationToken);

        if (incidents.Count == 0)
        {
            return $"Job '{jobNumber}' has no recorded quality/safety incidents.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Job {job.JobNumber} — quality/safety incidents:");

        foreach (var incident in incidents)
        {
            sb.AppendLine(
                $"- [{incident.Severity}] Reported {incident.ReportedAt:yyyy-MM-dd}: {incident.Description}");
        }

        return sb.ToString();
    }

    public async Task<string> GetProductsForJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobNumber == jobNumber, cancellationToken);

        if (job is null)
        {
            return $"No job was found with job number '{jobNumber}'.";
        }

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.JobId == job.Id)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return $"Job '{jobNumber}' has no recorded products/materials.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Job {job.JobNumber} — products/materials consumed:");

        foreach (var product in products)
        {
            sb.AppendLine($"- {product.Quantity} {product.UnitOfMeasure} of {product.Name}");
        }

        return sb.ToString();
    }

    public async Task<string> GetJobsByClientAndTypeAsync(
    string clientName,
    string? jobType,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientName))
        {
            return "Client name is required.";
        }

        var clientJobs = await _dbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Well!.Field)
            .Where(j => j.ClientName.Contains(clientName))
            .OrderBy(j => j.JobNumber)
            .ToListAsync(cancellationToken);

        if (clientJobs.Count == 0)
        {
            return $"No jobs were found for client '{clientName}'.";
        }

        if (string.IsNullOrWhiteSpace(jobType))
        {
            var allJobsSb = new StringBuilder();
            allJobsSb.AppendLine($"Jobs for client '{clientName}':");

            foreach (var job in clientJobs)
            {
                allJobsSb.AppendLine(
                    $"- {job.JobNumber} | {job.JobType} | {job.Status} | well: {job.Well?.Name} | field: {job.Well?.Field?.Name}");
            }

            return allJobsSb.ToString();
        }

        var matchingJobs = clientJobs
            .Where(j => string.Equals(j.JobType, jobType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(j => j.JobNumber)
            .ToList();

        var sb = new StringBuilder();

        if (matchingJobs.Count == 0)
        {
            sb.AppendLine($"No '{jobType}' jobs exist for client '{clientName}'.");
            sb.AppendLine($"Jobs recorded for client '{clientName}':");

            foreach (var job in clientJobs)
            {
                sb.AppendLine(
                    $"- {job.JobNumber} — {job.JobType}");
            }

            return sb.ToString();
        }

        sb.AppendLine($"'{jobType}' job(s) for client '{clientName}':");

        foreach (var job in matchingJobs)
        {
            sb.AppendLine(
                $"- {job.JobNumber} | status: {job.Status} | well: {job.Well?.Name} | field: {job.Well?.Field?.Name} | mobilized: {job.MobilizationDate:yyyy-MM-dd}");
        }

        return sb.ToString();
    }

    public async Task<string> GetFullJobReportAsync(
        string jobNumber,
        CancellationToken cancellationToken = default)
    {
        var details = await GetJobDetailsAsync(jobNumber, cancellationToken);

        if (details.StartsWith("No job was found", StringComparison.Ordinal))
        {
            return details;
        }

        var operations = await GetOperationsAndRunResultsAsync(jobNumber, cancellationToken);
        var personnel = await GetPersonnelForJobAsync(jobNumber, cancellationToken);
        var products = await GetProductsForJobAsync(jobNumber, cancellationToken);
        var incidents = await GetQualityIncidentsForJobAsync(jobNumber, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine(details);
        sb.AppendLine();
        sb.AppendLine(operations);
        sb.AppendLine();
        sb.AppendLine(personnel);
        sb.AppendLine();
        sb.AppendLine(products);
        sb.AppendLine();
        sb.AppendLine(incidents);

        return sb.ToString();
    }
}
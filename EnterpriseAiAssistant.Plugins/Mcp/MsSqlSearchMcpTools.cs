using System.ComponentModel;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using ModelContextProtocol.Server;

namespace EnterpriseAiAssistant.Plugins.Mcp;

/// <summary>
/// MCP exposure of <see cref="IJobSqlSearchService"/> — the same deterministic
/// SQL retrieval that <c>MsSqlSearchPlugin</c> offers to Semantic Kernel.
/// Both are thin adapters over the Application-layer interface, so there is
/// one implementation of the retrieval logic and two ways to call it.
/// </summary>
[McpServerToolType]
public sealed class MsSqlSearchMcpTools
{
    private const string Source = SourceSystemNames.SqlJobDatabase;

    private readonly IJobSqlSearchService _sql;
    private readonly McpResultGuard _guard;

    public MsSqlSearchMcpTools(IJobSqlSearchService sql, McpResultGuard guard)
    {
        _sql = sql;
        _guard = guard;
    }

    [McpServerTool(Name = "get_job_details", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Gets the exact structured details (status, client, well, dates) of a single job by its job number, e.g. 'JOB-2026-0001'.")]
    public async Task<string> GetJobDetails(
        [Description("The exact job number, e.g. 'JOB-2026-0001'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _sql.GetJobDetailsAsync(jobNumber, cancellationToken), jobNumber);

    [McpServerTool(Name = "list_jobs", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Lists jobs from the JOB source-of-truth database, optionally filtered by status, client name, well name, field name, and/or a mobilization date range. Use this for questions like 'which jobs are in progress', 'jobs for Apex Energy in January 2026', or 'which jobs ran on well PB-102'.")]
    public async Task<string> ListJobs(
        [Description("Optional job status filter: Scheduled, Mobilized, InProgress, Completed, or Cancelled.")] string? status = null,
        [Description("Optional client name filter (partial match).")] string? clientName = null,
        [Description("Optional well name filter (partial match), e.g. 'PB-102'.")] string? wellName = null,
        [Description("Optional field name filter (partial match), e.g. 'Permian Basin' or 'Bakken'.")] string? fieldName = null,
        [Description("Optional inclusive start of the mobilization date range, ISO 8601.")] DateTimeOffset? fromDate = null,
        [Description("Optional inclusive end of the mobilization date range, ISO 8601.")] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
        => _guard.Apply(
            Source,
            await _sql.ListJobsAsync(status, clientName, wellName, fieldName, fromDate, toDate, cancellationToken),
            $"{status} {clientName} {wellName} {fieldName}".Trim());

    [McpServerTool(Name = "count_jobs_by_status", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Returns an exact count of jobs, optionally filtered to a single status. Use this for questions like 'how many jobs are currently in progress?'.")]
    public async Task<string> CountJobsByStatus(
        [Description("Optional job status to count: Scheduled, Mobilized, InProgress, Completed, or Cancelled. Omit to count all jobs.")] string? status = null,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _sql.CountJobsByStatusAsync(status, cancellationToken), status ?? string.Empty);

    [McpServerTool(Name = "get_operations_and_run_results", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Returns the exact operations and runs (with pass/fail result, start/completion times) for a job, read directly from SQL Server. Use for 'what were the run results for job X' or 'which run failed on job X'.")]
    public async Task<string> GetOperationsAndRunResults(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _sql.GetOperationsAndRunResultsAsync(jobNumber, cancellationToken), jobNumber);

    [McpServerTool(Name = "get_personnel_for_job", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Returns the exact crew/personnel assigned to every operation and run of a job, read directly from SQL Server. Use for 'who worked on job X' or 'who was the crew lead for job X'.")]
    public async Task<string> GetPersonnelForJob(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _sql.GetPersonnelForJobAsync(jobNumber, cancellationToken), jobNumber);

    [McpServerTool(Name = "get_quality_incidents_for_job", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Returns the exact quality/safety incidents recorded for a job, including the full description text, severity, and reported date. Use for 'what does the quality incident say for job X' or 'was there an incident on job X'.")]
    public async Task<string> GetQualityIncidentsForJob(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _sql.GetQualityIncidentsForJobAsync(jobNumber, cancellationToken), jobNumber);

    [McpServerTool(Name = "get_products_for_job", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Returns the exact products/materials consumed on a job with quantities and units of measure. Use for 'what products were used on job X' or 'how much proppant was used on job X'.")]
    public async Task<string> GetProductsForJob(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _sql.GetProductsForJobAsync(jobNumber, cancellationToken), jobNumber);

    [McpServerTool(Name = "get_full_job_report", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Returns one complete deterministic report for a job: metadata, operations/runs/results, personnel, products, and quality incidents. Use for broad questions like 'tell me everything about job X' or 'give me a full summary of job X'.")]
    public async Task<string> GetFullJobReport(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _sql.GetFullJobReportAsync(jobNumber, cancellationToken), jobNumber);

    [McpServerTool(Name = "get_operation_count_by_field", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Returns the exact total operation count (and job count) per production field, computed by joining Job -> Well -> Field directly in SQL Server. Use this for questions like 'how many operations were performed in the Bakken field compared to Permian Basin' or 'total operations by field'.")]
    public async Task<string> GetOperationCountByField(
        [Description("Optional list of field names to compare, e.g. ['Bakken', 'Permian Basin']. Omit or leave empty to include all fields.")] IReadOnlyList<string>? fieldNames = null,
        CancellationToken cancellationToken = default)
        => _guard.Apply(
            Source,
            await _sql.GetOperationCountByFieldAsync(fieldNames ?? [], cancellationToken),
            fieldNames is { Count: > 0 } ? string.Join(", ", fieldNames) : string.Empty);

    [McpServerTool(Name = "get_jobs_by_client_and_type", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[SQL] Returns the exact jobs for a client, optionally filtered to a specific job type. Use this for questions like 'Show me all jobs of type Stimulation for Apex Energy'. If no jobs match the type, it returns that fact and also lists the jobs that do exist for the client.")]
    public async Task<string> GetJobsByClientAndType(
        [Description("The client name, e.g. 'Apex Energy'.")] string clientName,
        [Description("Optional exact job type, e.g. 'Stimulation', 'Completion', 'Wireline Logging', 'Workover', or 'Perforation'.")] string? jobType = null,
        CancellationToken cancellationToken = default)
        => _guard.Apply(Source, await _sql.GetJobsByClientAndTypeAsync(clientName, jobType, cancellationToken), clientName);
}

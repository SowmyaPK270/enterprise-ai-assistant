using System.ComponentModel;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using Microsoft.SemanticKernel;

namespace EnterpriseAiAssistant.Plugins.Tools;

/// <summary>
/// Semantic Kernel plugin wrapper around <see cref="IJobSqlSearchService"/>.
/// Deterministic relational retrieval against SQL Server, the single
/// source of truth for JOB data: exact facts, counts, dates, filtering,
/// aggregations. No embeddings or AI-based similarity search are used.
/// </summary>
public sealed class MsSqlSearchPlugin
{
    private readonly IJobSqlSearchService _jobSqlSearchService;

    public MsSqlSearchPlugin(IJobSqlSearchService jobSqlSearchService)
    {
        _jobSqlSearchService = jobSqlSearchService;
    }

    [KernelFunction("get_job_details")]
    [Description("Gets the exact structured details (status, client, well, dates) of a single job by its job number, e.g. 'JOB-2026-0001'.")]
    public Task<string> GetJobDetailsAsync(
        [Description("The exact job number, e.g. 'JOB-2026-0001'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _jobSqlSearchService.GetJobDetailsAsync(jobNumber, cancellationToken);

    [KernelFunction("list_jobs")]
    [Description("Lists jobs from the JOB source-of-truth database, optionally filtered by status, client name, well name, field name, and/or a mobilization date range. Use this for questions like 'which jobs are in progress', 'jobs for Apex Energy in January 2026', or 'which jobs ran on well PB-102'.")]
    public Task<string> ListJobsAsync(
    [Description("Optional job status filter: Scheduled, Mobilized, InProgress, Completed, or Cancelled.")] string? status = null,
    [Description("Optional client name filter (partial match).")] string? clientName = null,
    [Description("Optional well name filter (partial match), e.g. 'PB-102'.")] string? wellName = null,
    [Description("Optional field name filter (partial match), e.g. 'Permian Basin' or 'Bakken'.")] string? fieldName = null,
    [Description("Optional inclusive start of the mobilization date range, ISO 8601.")] DateTimeOffset? fromDate = null,
    [Description("Optional inclusive end of the mobilization date range, ISO 8601.")] DateTimeOffset? toDate = null,
    CancellationToken cancellationToken = default)
    => _jobSqlSearchService.ListJobsAsync(status, clientName, wellName, fieldName, fromDate, toDate, cancellationToken);

    [KernelFunction("count_jobs_by_status")]
    [Description("Returns an exact count of jobs, optionally filtered to a single status. Use this for questions like 'how many jobs are currently in progress?'.")]
    public Task<string> CountJobsByStatusAsync(
        [Description("Optional job status to count: Scheduled, Mobilized, InProgress, Completed, or Cancelled. Omit to count all jobs.")] string? status = null,
        CancellationToken cancellationToken = default)
        => _jobSqlSearchService.CountJobsByStatusAsync(status, cancellationToken);

    [KernelFunction("get_operations_and_run_results")]
    [Description("Returns the exact operations and runs (with pass/fail result, start/completion times) for a job, read directly from SQL Server. Use for 'what were the run results for job X' or 'which run failed on job X'.")]
    public Task<string> GetOperationsAndRunResultsAsync(
    [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
    CancellationToken cancellationToken = default)
    => _jobSqlSearchService.GetOperationsAndRunResultsAsync(jobNumber, cancellationToken);

    [KernelFunction("get_personnel_for_job")]
    [Description("Returns the exact crew/personnel assigned to every operation and run of a job, read directly from SQL Server. Use for 'who worked on job X' or 'who was the crew lead for job X'.")]
    public Task<string> GetPersonnelForJobAsync(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _jobSqlSearchService.GetPersonnelForJobAsync(jobNumber, cancellationToken);

    [KernelFunction("get_quality_incidents_for_job")]
    [Description("Returns the exact quality/safety incidents recorded for a job, including the full description text, severity, and reported date. Use for 'what does the quality incident say for job X' or 'was there an incident on job X'.")]
    public Task<string> GetQualityIncidentsForJobAsync(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _jobSqlSearchService.GetQualityIncidentsForJobAsync(jobNumber, cancellationToken);

    [KernelFunction("get_products_for_job")]
    [Description("Returns the exact products/materials consumed on a job with quantities and units of measure. Use for 'what products were used on job X' or 'how much proppant was used on job X'.")]
    public Task<string> GetProductsForJobAsync(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _jobSqlSearchService.GetProductsForJobAsync(jobNumber, cancellationToken);

    [KernelFunction("get_full_job_report")]
    [Description("Returns one complete deterministic report for a job: metadata, operations/runs/results, personnel, products, and quality incidents. Use for broad questions like 'tell me everything about job X' or 'give me a full summary of job X'.")]
    public Task<string> GetFullJobReportAsync(
        [Description("The exact job number, e.g. 'JOB-2026-0004'.")] string jobNumber,
        CancellationToken cancellationToken = default)
        => _jobSqlSearchService.GetFullJobReportAsync(jobNumber, cancellationToken);

    [KernelFunction("get_operation_count_by_field")]
    [Description("Returns the exact total operation count (and job count) per production field, computed by joining Job -> Well -> Field directly in SQL Server. Use this for questions like 'how many operations were performed in the Bakken field compared to Permian Basin' or 'total operations by field'.")]
    public Task<string> GetOperationCountByFieldAsync(
        [Description("Optional list of field names to compare, e.g. ['Bakken', 'Permian Basin']. Omit or leave empty to include all fields.")] IReadOnlyList<string>? fieldNames = null,
        CancellationToken cancellationToken = default)
        => _jobSqlSearchService.GetOperationCountByFieldAsync(fieldNames ?? [], cancellationToken);

    [KernelFunction("get_jobs_by_client_and_type")]
    [Description("Returns the exact jobs for a client, optionally filtered to a specific job type. Use this for questions like 'Show me all jobs of type Stimulation for Apex Energy'. If no jobs match the type, it returns that fact and also lists the jobs that do exist for the client.")]
    public Task<string> GetJobsByClientAndTypeAsync(
    [Description("The client name, e.g. 'Apex Energy'.")] string clientName,
    [Description("Optional exact job type, e.g. 'Stimulation', 'Completion', 'Wireline Logging', 'Workover', or 'Perforation'.")] string? jobType = null,
    CancellationToken cancellationToken = default)
    => _jobSqlSearchService.GetJobsByClientAndTypeAsync(clientName, jobType, cancellationToken);

}

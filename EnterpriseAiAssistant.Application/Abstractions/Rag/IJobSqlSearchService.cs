namespace EnterpriseAiAssistant.Application.Abstractions.Rag;

/// <summary>
/// Deterministic relational retrieval against SQL Server, the single
/// source of truth for JOB data: exact facts, counts, dates, filtering,
/// aggregations. No embeddings or AI-based similarity search are used
/// here — every method maps directly to a relational query and the
/// formatted result is returned as retrieved context for the LLM.
/// Backs the Semantic Kernel "MsSqlSearch" plugin.
/// </summary>
public interface IJobSqlSearchService
{
    /// <summary>
    /// Exact lookup of a single job's structured details by job number.
    /// </summary>
    Task<string> GetJobDetailsAsync(
        string jobNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtered listing of jobs. Any combination of filters may be null,
    /// in which case that filter is not applied.
    /// </summary>
    Task<string> ListJobsAsync(
     string? status,
     string? clientName,
     string? wellName,
     string? fieldName,
     DateTimeOffset? fromDate,
     DateTimeOffset? toDate,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// Deterministic aggregation: count of jobs matching an optional
    /// status filter (e.g. "how many jobs are currently InProgress?").
    /// </summary>
    Task<string> CountJobsByStatusAsync(
        string? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exact listing of every operation and run (with result), read
    /// directly from SQL Server. Use this for Job -> Operation -> Run ->
    /// Result questions independent of the Cosmos DB graph.
    /// </summary>
    Task<string> GetOperationsAndRunResultsAsync(
        string jobNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exact personnel/crew assigned to every run of a job, read directly
    /// from SQL Server (job -> operation -> run -> personnel).
    /// </summary>
    Task<string> GetPersonnelForJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exact quality/safety incidents recorded for a job, including the
    /// full description text, severity, and reported date.
    /// </summary>
    Task<string> GetQualityIncidentsForJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exact products/materials consumed on a job with quantities and
    /// units of measure.
    /// </summary>
    Task<string> GetProductsForJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A single, complete deterministic report for a job: metadata,
    /// operations/runs/results, personnel, products, and quality
    /// incidents in one call. Use this when the question spans multiple
    /// categories (e.g. "tell me everything about job X").
    /// </summary>
    Task<string> GetFullJobReportAsync(
        string jobNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deterministic aggregation: total operation count (and job count)
    /// for every job whose Well belongs to the given Field, comparing
    /// across fields when multiple field names are supplied. Reads the
    /// Job -> Well -> Field relationship directly from SQL Server so
    /// field-level questions never require a join the LLM has to guess
    /// at from unstructured snippets.
    /// </summary>
    Task<string> GetOperationCountByFieldAsync(
        IReadOnlyList<string> fieldNames,
        CancellationToken cancellationToken = default);

    /// 
    /// Deterministic retrieval: fetches jobs filtered by the specified client 
    /// name and an optional job type. Queries client and job records directly from 
    /// SQL Server to ensure accurate, structured filtering without requiring 
    /// the LLM to guess relationships from unstructured text snippets.
    /// 
    Task<string> GetJobsByClientAndTypeAsync(
    string clientName,
    string? jobType,
    CancellationToken cancellationToken = default);
}
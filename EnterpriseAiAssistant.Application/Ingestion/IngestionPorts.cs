using EnterpriseAiAssistant.Domain.Ingestion;

namespace EnterpriseAiAssistant.Application.Ingestion;

/// <summary>
/// Reads the SQL Server JOB source-of-truth data that needs to be
/// ingested into the derived stores (Cosmos DB, Azure AI Search).
/// </summary>
public interface IJobDataSourceReader
{
    Task<IReadOnlyList<JobRecord>> GetAllJobsAsync(
        CancellationToken cancellationToken = default);

    Task<JobRecord?> GetJobAsync(
        string jobNumber,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Transforms a JobRecord into the Cosmos DB relationship/graph
/// representation (Job → Operation → Run → Personnel, and other entity
/// relationships) and upserts it. No embeddings are involved.
/// </summary>
public interface IJobGraphIndexWriter
{
    Task SyncJobGraphAsync(
        JobRecord job,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Chunks a JobRecord into job-evidence passages, embeds each chunk,
/// and writes them to the Azure AI Search job-evidence index so they
/// can be found by semantic/vector search even when the exact wording
/// of the question does not appear in the source data.
/// </summary>
public interface IJobEvidenceIndexWriter
{
    Task IndexJobEvidenceAsync(
        JobRecord job,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Chunks extracted document text, embeds each chunk, and writes them
/// to the Azure AI Search document index.
/// </summary>
public interface IDocumentIndexWriter
{
    Task IndexDocumentAsync(
        string fileName,
        string extractedText,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Extracts plain text from an uploaded document (PDF, DOCX, or plain
/// text) so it can be chunked and embedded.
/// </summary>
public interface IDocumentTextExtractor
{
    /// <summary>
    /// Returns true if this extractor can handle the given content
    /// type / file extension.
    /// </summary>
    bool CanHandle(string fileName, string contentType);

    Task<string> ExtractTextAsync(
        Stream content,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Tracks the status of every ingestion attempt so progress can be
/// observed and failures can be identified independently of the live
/// chat request path.
/// </summary>
public interface IIngestionStatusStore
{
    Task<IngestionRun> StartAsync(
        IngestionSourceType sourceType,
        string target,
        CancellationToken cancellationToken = default);

    Task MarkSucceededAsync(
        Guid ingestionRunId,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid ingestionRunId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IngestionRun>> GetRecentAsync(
        int count = 50,
        CancellationToken cancellationToken = default);
}

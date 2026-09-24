namespace EnterpriseAiAssistant.Application.Abstractions.Rag;

/// <summary>
/// Semantic/vector retrieval against Azure AI Search. The user's query
/// is embedded and compared against pre-computed vectors so relevant
/// content can be found even when the exact words do not appear in the
/// source. Two use cases share the same index but are distinguished by
/// their SourceType metadata: job-evidence derived from the SQL Server
/// JOB data, and unstructured PDFs/documents uploaded directly.
/// Backs the Semantic Kernel "AzureVectorSearch" plugin.
/// </summary>
public interface IAzureVectorSearchService
{
    /// <summary>
    /// Semantically relevant job-evidence snippets derived from
    /// structured SQL Server JOB data.
    /// </summary>
    Task<string> SearchJobEvidenceAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Semantically relevant snippets from uploaded PDFs/documents
    /// (e.g. maintenance manuals).
    /// </summary>
    Task<string> SearchDocumentsAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

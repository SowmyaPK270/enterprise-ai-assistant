using System.ComponentModel;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using ModelContextProtocol.Server;

namespace EnterpriseAiAssistant.Plugins.Mcp;

/// <summary>
/// MCP exposure of <see cref="IAzureVectorSearchService"/> — the same hybrid
/// semantic search that <c>AzureVectorSearchPlugin</c> offers to Semantic
/// Kernel, over both corpora in the shared index (SQL-derived job evidence
/// and uploaded PDFs/DOCX). All tools are read-only.
/// </summary>
[McpServerToolType]
public sealed class AzureVectorSearchMcpTools
{
    // Every call costs an embedding request; external clients are not
    // bound by the model's own restraint, so cap what they can ask for.
    private const int MinTopK = 1;
    private const int MaxTopK = 20;

    private readonly IAzureVectorSearchService _vector;
    private readonly McpResultGuard _guard;

    public AzureVectorSearchMcpTools(IAzureVectorSearchService vector, McpResultGuard guard)
    {
        _vector = vector;
        _guard = guard;
    }

    [McpServerTool(Name = "search_job_evidence", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[Vector] Semantic search over job-evidence derived from the SQL Server JOB data. Use this for open-ended or conceptual questions about jobs, operations, incidents, or products where the exact wording may not match the structured data (e.g. 'were there any pressure problems recently').")]
    public async Task<string> SearchJobEvidence(
        [Description("The natural-language question or topic to search for.")] string query,
        [Description("Maximum number of results to return (1-20).")] int topK = 5,
        CancellationToken cancellationToken = default)
        => _guard.Apply(
            SourceSystemNames.AzureVectorSearchJobEvidence,
            await _vector.SearchJobEvidenceAsync(query, Math.Clamp(topK, MinTopK, MaxTopK), cancellationToken),
            query);

    [McpServerTool(Name = "search_documents", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("[Vector] Semantic search over uploaded PDFs/documents such as maintenance manuals. Use this when the question is about procedures, specifications, or reference material rather than a specific job's data.")]
    public async Task<string> SearchDocuments(
        [Description("The natural-language question or topic to search for.")] string query,
        [Description("Maximum number of results to return (1-20).")] int topK = 5,
        CancellationToken cancellationToken = default)
        => _guard.Apply(
            SourceSystemNames.AzureVectorSearchDocuments,
            await _vector.SearchDocumentsAsync(query, Math.Clamp(topK, MinTopK, MaxTopK), cancellationToken),
            query);
}

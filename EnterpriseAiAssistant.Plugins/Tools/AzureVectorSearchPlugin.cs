using System.ComponentModel;
using EnterpriseAiAssistant.Application.Abstractions.Rag;
using Microsoft.SemanticKernel;

namespace EnterpriseAiAssistant.Plugins.Tools;

/// <summary>
/// Semantic Kernel plugin wrapper around <see cref="IAzureVectorSearchService"/>.
/// Embeds the query and finds semantically relevant content in Azure AI
/// Search even when the exact wording does not appear in the source
/// data — covering both job-evidence derived from SQL Server and
/// uploaded PDFs/documents such as maintenance manuals.
/// </summary>
public sealed class AzureVectorSearchPlugin
{
    private readonly IAzureVectorSearchService _azureVectorSearchService;

    public AzureVectorSearchPlugin(IAzureVectorSearchService azureVectorSearchService)
    {
        _azureVectorSearchService = azureVectorSearchService;
    }

    [KernelFunction("search_job_evidence")]
    [Description("Semantic search over job-evidence derived from the SQL Server JOB data. Use this for open-ended or conceptual questions about jobs, operations, incidents, or products where the exact wording may not match the structured data (e.g. 'were there any pressure problems recently').")]
    public Task<string> SearchJobEvidenceAsync(
        [Description("The natural-language question or topic to search for.")] string query,
        [Description("Maximum number of results to return.")] int topK = 5,
        CancellationToken cancellationToken = default)
        => _azureVectorSearchService.SearchJobEvidenceAsync(query, topK, cancellationToken);

    [KernelFunction("search_documents")]
    [Description("Semantic search over uploaded PDFs/documents such as maintenance manuals. Use this when the question is about procedures, specifications, or reference material rather than a specific job's data.")]
    public Task<string> SearchDocumentsAsync(
        [Description("The natural-language question or topic to search for.")] string query,
        [Description("Maximum number of results to return.")] int topK = 5,
        CancellationToken cancellationToken = default)
        => _azureVectorSearchService.SearchDocumentsAsync(query, topK, cancellationToken);
}

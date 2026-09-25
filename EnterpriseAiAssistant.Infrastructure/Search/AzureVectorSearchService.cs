using System.Text;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EnterpriseAiAssistant.Application.Abstractions.Embeddings;
using EnterpriseAiAssistant.Application.Abstractions.Rag;

namespace EnterpriseAiAssistant.Infrastructure.Search;

/// <summary>
/// Backs the "AzureVectorSearch" Semantic Kernel plugin. The user's
/// question is embedded with the same model used at ingestion time,
/// then compared against the pre-computed vectors in the shared
/// job-knowledge index. SourceType filters the same index down to just
/// job-evidence or just documents depending on which method is called.
/// </summary>
public sealed class AzureVectorSearchService : IAzureVectorSearchService
{
    private readonly SearchClientFactory _clientFactory;
    private readonly IEmbeddingGenerator _embeddingGenerator;

    public AzureVectorSearchService(
        SearchClientFactory clientFactory,
        IEmbeddingGenerator embeddingGenerator)
    {
        _clientFactory = clientFactory;
        _embeddingGenerator = embeddingGenerator;
    }

    public Task<string> SearchJobEvidenceAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(query, JobKnowledgeSourceTypes.JobEvidence, topK, cancellationToken);
    }

    public Task<string> SearchDocumentsAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(query, JobKnowledgeSourceTypes.Document, topK, cancellationToken);
    }

    private async Task<string> SearchAsync(
    string query,
    string sourceType,
    int topK,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "No search query was provided.";
        }

        var queryVector = await _embeddingGenerator.GenerateAsync(query, cancellationToken);

        var searchClient = _clientFactory.GetSearchClient();

        var vectorQuery = new VectorizedQuery(queryVector)
        {
            KNearestNeighborsCount = topK,
            Fields = { "ContentVector" }
        };

        var options = new SearchOptions
        {
            VectorSearch = new VectorSearchOptions { Queries = { vectorQuery } },
            Filter = $"SourceType eq '{EscapeODataLiteral(sourceType)}'",
            Size = topK,
            Select = { "Content", "JobNumber", "DocumentName" },
            QueryType = SearchQueryType.Simple,
            SearchFields = { "Content" }
        };

        // Hybrid search: This catches exact terms
        // (e.g. "Failure", "Flowback") that a pure vector search can
        // sometimes under-rank.
        var response = await searchClient.SearchAsync<SearchDocument>(
            searchText: query, options, cancellationToken);

        var sb = new StringBuilder();
        var hitCount = 0;

        await foreach (var result in response.Value.GetResultsAsync())
        {
            hitCount++;

            var content = result.Document.TryGetValue("Content", out var c) ? c?.ToString() : null;
            var jobNumber = result.Document.TryGetValue("JobNumber", out var j) ? j?.ToString() : null;
            var documentName = result.Document.TryGetValue("DocumentName", out var d) ? d?.ToString() : null;

            var reference = !string.IsNullOrEmpty(jobNumber)
                ? $"job {jobNumber}"
                : !string.IsNullOrEmpty(documentName)
                    ? $"document '{documentName}'"
                    : "unknown source";

            sb.AppendLine($"[Relevance {result.Score:0.000}, source: {reference}] {content}");
        }

        return hitCount == 0
            ? "No semantically relevant results were found."
            : sb.ToString();
    }

    private static string EscapeODataLiteral(string value) => value.Replace("'", "''");
}

using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace EnterpriseAiAssistant.Infrastructure.Search;

/// <summary>
/// Idempotently ensures the shared job-knowledge index exists with the
/// schema both writers (job-evidence and documents) and the
/// AzureVectorSearch reader depend on. 
/// </summary>
public sealed class JobKnowledgeIndexInitializer
{
    private const string VectorProfileName = "job-knowledge-vector-profile";
    private const string VectorAlgorithmName = "job-knowledge-hnsw";

    // text-embedding-3-small produces 1536-dimensional vectors. Update
    // this if a different embedding deployment/model is configured.
    private const int VectorDimensions = 1536;

    private readonly SearchClientFactory _clientFactory;

    public JobKnowledgeIndexInitializer(SearchClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        var fields = new List<SearchField>
        {
            new SimpleField("Id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
            new SearchableField("Content"),
            new SimpleField("SourceType", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
            new SimpleField("JobNumber", SearchFieldDataType.String) { IsFilterable = true },
            new SimpleField("DocumentName", SearchFieldDataType.String) { IsFilterable = true },
            new SimpleField("CreatedAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
            new VectorSearchField("ContentVector", VectorDimensions, VectorProfileName)
        };

        var vectorSearch = new VectorSearch();
        vectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(VectorAlgorithmName));
        vectorSearch.Profiles.Add(new VectorSearchProfile(VectorProfileName, VectorAlgorithmName));

        var index = new SearchIndex(_clientFactory.IndexName)
        {
            Fields = fields,
            VectorSearch = vectorSearch
        };

        await _clientFactory.IndexClient.CreateOrUpdateIndexAsync(
            index, cancellationToken: cancellationToken);
    }
}
